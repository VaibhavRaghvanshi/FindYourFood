using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FindYourFood.Contracts;
using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Interfaces;
using FindYourFood.Payment.Domain;

namespace FindYourFood.Payment.Application.Services;

public sealed class Payments(IPaymentRepository repository, IUnitOfWork unitOfWork,
    IOrderPaymentAccess orders, IPaymentGateway gateway, TimeProvider clock) : IIntegrationEventHandler
{
    public async Task<PaymentResult> GetAsync(PaymentActor actor, Guid order, CancellationToken ct)
    {
        var intent = await repository.OrderAsync(order, ct);
        return intent is null || !CanRead(actor, intent) ? new(PaymentResultStatus.Missing)
            : new(PaymentResultStatus.Accepted, View(intent));
    }

    public async Task<PaymentResult> SimulateAsync(PaymentActor actor, Guid order, SimulatePayment input, CancellationToken ct)
    {
        if (input.IdempotencyKey == Guid.Empty || input.ExpectedTotalMinor is < 1 or > 100_100_000)
            return new(PaymentResultStatus.Invalid);
        var intent = await repository.OrderAsync(order, ct);
        if (intent is null || !CanRead(actor, intent)) return new(PaymentResultStatus.Missing);
        if (intent.TotalMinor != input.ExpectedTotalMinor) return new(PaymentResultStatus.Conflict);
        var existing = await repository.ByKeyAsync(intent.Id, input.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.SimulationSuccess != input.Success) return new(PaymentResultStatus.Conflict);
            if (existing.State == AttemptState.Pending) await ProcessAttemptAsync(existing.Id, ct);
            return await GetAsync(actor, order, ct);
        }
        if (intent.SuccessfulPaymentId is not null) return new(PaymentResultStatus.Accepted, View(intent));
        var authority = await orders.GetAsync(actor, order, ct);
        if (authority is null) return new(PaymentResultStatus.Missing);
        if (authority.Status != "AwaitingPayment" || clock.GetUtcNow() >= intent.Deadline ||
            authority.TotalMinor != intent.TotalMinor || authority.Currency != intent.Currency)
            return new(PaymentResultStatus.Conflict);
        var attempt = new PaymentAttempt(Guid.NewGuid(), intent.Id, input.IdempotencyKey, input.Success, clock.GetUtcNow());
        if (!intent.Begin(attempt.Id)) return new(PaymentResultStatus.Conflict);
        repository.Add(attempt);
        if (!await unitOfWork.TrySaveAsync(ct)) return new(PaymentResultStatus.Conflict);
        await ProcessAttemptAsync(attempt.Id, ct);
        return await GetAsync(actor, order, ct);
    }

    public async Task<PendingPaymentWork> PendingAsync(CancellationToken ct) =>
        new(await repository.PendingAttemptsAsync(clock.GetUtcNow(), ct),
            await repository.PendingRefundsAsync(clock.GetUtcNow(), ct));

    public async Task ProcessAttemptAsync(Guid id, CancellationToken ct)
    {
        var attempt = await repository.AttemptAsync(id, ct);
        if (attempt is null || !attempt.Claim(clock.GetUtcNow())) return;
        if (!await unitOfWork.TrySaveAsync(ct)) return;
        var intent = await repository.IntentAsync(attempt.IntentId, ct)
            ?? throw new InvalidOperationException("Missing payment intent.");
        var result = await gateway.ChargeAsync(new(attempt.Id, intent.TotalMinor, intent.Currency, attempt.SimulationSuccess), ct);
        await CompleteAsync(id, result, null, ct);
    }

    private async Task CompleteAsync(Guid id, GatewayResult result, InboxReceipt? receipt, CancellationToken ct)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var attempt = await repository.AttemptAsync(id, ct)
                ?? throw new InvalidOperationException("Missing payment attempt.");
            var intent = await repository.IntentAsync(attempt.IntentId, ct)
                ?? throw new InvalidOperationException("Missing payment intent.");
            var now = clock.GetUtcNow();
            if (attempt.Complete(result.Success, result.Reference, now))
            {
                intent.Complete(attempt.Id, result.Success);
                if (result.Success)
                    Enqueue(intent, "payment.succeeded.v1", new PaymentSucceeded(intent.OrderId, attempt.Id, intent.TotalMinor, intent.Currency), now);
                else Enqueue(intent, "payment.failed.v1", new PaymentFailed(intent.OrderId, attempt.Id, "declined"), now);
            }
            else
            {
                if ((attempt.State == AttemptState.Succeeded) != result.Success)
                    throw new InvalidIntegrationEventException("gateway-outcome-conflict");
                if (receipt is null) return;
            }
            if (receipt is not null) repository.Add(receipt);
            if (await unitOfWork.TrySaveAsync(ct)) return;
            if (receipt is not null && await repository.InboxHashAsync(receipt.MessageId, ct) == receipt.ContentHash) return;
        }
        throw new InvalidOperationException("Payment completion will retry.");
    }

    public async Task ProcessRefundAsync(Guid payment, CancellationToken ct)
    {
        var refund = await repository.RefundAsync(payment, ct);
        if (refund is null || !refund.Claim(clock.GetUtcNow())) return;
        if (!await unitOfWork.TrySaveAsync(ct)) return;
        var attempt = await repository.AttemptAsync(payment, ct)
            ?? throw new InvalidOperationException("Missing refund payment.");
        var result = await gateway.RefundAsync(new(refund.Id, payment, refund.TotalMinor, refund.Currency,
            attempt.ProviderReference!), ct);
        if (!result.Success) throw new HttpRequestException("Refund gateway will retry.");
        for (var retry = 0; retry < 3; retry++)
        {
            refund = await repository.RefundAsync(payment, ct);
            if (refund is null || refund.Completed) return;
            attempt = await repository.AttemptAsync(payment, ct);
            var intent = await repository.IntentAsync(attempt!.IntentId, ct);
            refund.Complete(result.Reference);
            intent!.Refunded(payment);
            Enqueue(intent, "payment.refunded.v1", new PaymentRefunded(intent.OrderId, payment), clock.GetUtcNow());
            if (await unitOfWork.TrySaveAsync(ct)) return;
        }
        throw new InvalidOperationException("Refund completion will retry.");
    }

    public async Task HandleAsync(IntegrationEvent message, CancellationToken ct)
    {
        if (!message.IsValid() || message.Source != "ordering" ||
            message.Type is not ("ordering.payment-requested.v1" or "ordering.payment-cancelled.v1" or "ordering.refund-requested.v1"))
            throw new InvalidIntegrationEventException("unsupported-ordering-event");
        var hash = message.ContentHash();
        var existing = await repository.InboxHashAsync(message.MessageId, ct);
        if (existing is not null)
        {
            if (existing != hash) throw new InvalidIntegrationEventException("message-id-collision");
            return;
        }
        var now = clock.GetUtcNow();
        var intent = await repository.OrderAsync(message.AggregateId, ct);
        switch (message.Type)
        {
            case "ordering.payment-requested.v1":
                var requested = ReadRequest(message);
                if (requested.OrderId != message.AggregateId || requested.TotalMinor is < 1 or > 100_100_000 ||
                    requested.Currency != "INR" || requested.CustomerId == Guid.Empty ||
                    (requested.CustomerId is null && requested.GuestKeyHash is not { Length: 64 }))
                    throw new InvalidIntegrationEventException("invalid-payment-request");
                if (intent is null)
                {
                    intent = new PaymentIntent(Guid.NewGuid(), requested.OrderId, requested.CustomerId,
                        requested.GuestKeyHash, requested.TotalMinor, requested.Currency, requested.PaymentDeadline, now);
                    if (requested.PaymentDeadline <= now || await repository.WasCancelledAsync(requested.OrderId, ct)) intent.Cancel();
                    repository.Add(intent);
                }
                else if (intent.TotalMinor != requested.TotalMinor || intent.Currency != requested.Currency ||
                    intent.CustomerId != requested.CustomerId || intent.GuestKeyHash != requested.GuestKeyHash)
                    throw new InvalidIntegrationEventException("payment-request-conflict");
                break;
            case "ordering.payment-cancelled.v1":
                if (intent is not null) intent.Cancel();
                else if (!await repository.WasCancelledAsync(message.AggregateId, ct))
                    repository.Add(new CancelledOrder(message.AggregateId, now));
                break;
            case "ordering.refund-requested.v1":
                var requestedRefund = message.Data.Deserialize<RefundRequested>(IntegrationEvent.Json)
                    ?? throw new InvalidIntegrationEventException("missing-refund");
                var attempt = await repository.AttemptAsync(requestedRefund.PaymentId, ct);
                if (intent is null || requestedRefund.OrderId != message.AggregateId || attempt?.IntentId != intent.Id ||
                    attempt.State != AttemptState.Succeeded)
                    throw new InvalidIntegrationEventException("invalid-refund");
                if (await repository.RefundAsync(attempt.Id, ct) is null)
                    repository.Add(new RefundInstruction(Guid.NewGuid(), attempt.Id, intent.OrderId, intent.TotalMinor, intent.Currency));
                break;
        }
        repository.Add(new InboxReceipt(message.MessageId, message.Source, message.Type, hash, now));
        if (!await unitOfWork.TrySaveAsync(ct) && await repository.InboxHashAsync(message.MessageId, ct) != hash)
            throw new InvalidOperationException("Payment event processing will retry.");
    }

    public async Task HandleGatewayAsync(GatewayCompletion completion, CancellationToken ct)
    {
        if (completion.EventId == Guid.Empty || completion.PaymentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(completion.Reference) || completion.Reference.Length > 100 ||
            completion.Reference.Any(char.IsControl))
            throw new InvalidIntegrationEventException("invalid-gateway-event");
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(completion)));
        var previous = await repository.InboxHashAsync(completion.EventId, ct);
        if (previous is not null)
        {
            if (previous != fingerprint) throw new InvalidIntegrationEventException("gateway-event-id-collision");
            return;
        }
        var attempt = await repository.AttemptAsync(completion.PaymentId, ct)
            ?? throw new InvalidIntegrationEventException("unknown-payment");
        var intent = await repository.IntentAsync(attempt.IntentId, ct);
        if (intent!.TotalMinor != completion.TotalMinor || intent.Currency != completion.Currency)
            throw new InvalidIntegrationEventException("gateway-amount-mismatch");
        await CompleteAsync(attempt.Id, new(completion.Success, completion.Reference),
            new(completion.EventId, "gateway", "gateway.completed.v1", fingerprint, clock.GetUtcNow()), ct);
    }

    private static PaymentRequested ReadRequest(IntegrationEvent message)
    {
        var requested = message.Data.Deserialize<PaymentRequested>(IntegrationEvent.Json)
            ?? throw new InvalidIntegrationEventException("missing-payment-request");
        if (requested.OrderId != Guid.Empty) return requested;
        foreach (var property in message.Data.EnumerateObject())
        {
            if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && property.Value.TryGetGuid(out var id))
                return requested with { OrderId = id, PaymentDeadline = message.OccurredAt.AddMinutes(15) };
        }
        return requested;
    }

    private void Enqueue(PaymentIntent intent, string type, object payload, DateTimeOffset now) =>
        repository.Add(new IntegrationEvent(Guid.NewGuid(), "payment", type, intent.OrderId, intent.Revision, now,
            JsonSerializer.SerializeToElement(payload, IntegrationEvent.Json)));

    private static bool CanRead(PaymentActor actor, PaymentIntent intent) =>
        intent.CustomerId is Guid customer ? actor.CustomerId == customer :
            actor.GuestKey is { Length: 64 } key && intent.GuestKeyHash is { } hash &&
                CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash),
                    Encoding.UTF8.GetBytes(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))));

    private static PaymentView View(PaymentIntent intent) =>
        new(intent.Id, intent.OrderId, intent.State.ToString(), intent.TotalMinor, intent.Currency, intent.SuccessfulPaymentId, intent.Deadline);
}
