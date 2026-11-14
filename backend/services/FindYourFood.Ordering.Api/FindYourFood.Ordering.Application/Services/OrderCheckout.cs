using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Domain;
using FindYourFood.Contracts;

namespace FindYourFood.Ordering.Application.Services;

public sealed record CheckoutPolicy(TimeSpan PaymentWindow);

public sealed class OrderCheckout(IOrderRepository orders, ICartCheckout carts, IUnitOfWork unitOfWork,
    TimeProvider clock, CheckoutPolicy policy) : IIntegrationEventHandler
{
    public async Task<OrderResult> CreateAsync(OrderActor actor, CheckoutRequest input, CancellationToken ct)
    {
        if (input.IdempotencyKey == Guid.Empty || input.CartId == Guid.Empty || input.ExpectedCartRevision < 1 ||
            input.ExpectedTotalMinor is < 1 or > 100_100_000 || input.Delivery?.IsValid() != true)
        {
            return new(OrderResultStatus.Invalid);
        }
        var scope = Scope(actor);
        if (scope is null) return new(OrderResultStatus.Missing);
        var delivery = input.Delivery.Normalized();
        var fingerprint = Hash(JsonSerializer.Serialize(new
        {
            input.CartId, input.ExpectedCartRevision, input.ExpectedTotalMinor, delivery
        }));
        var existing = await orders.ByKeyAsync(scope, input.IdempotencyKey, ct)
            ?? await orders.ByCartAsync(input.CartId, input.ExpectedCartRevision, ct);
        if (existing is not null) return await ReplayAsync(actor, existing, fingerprint, ct);

        CheckoutQuote quote;
        try { quote = await carts.QuoteAsync(actor, input with { Delivery = delivery }, ct); }
        catch (CheckoutMissingException) { return new(OrderResultStatus.Missing); }
        catch (CheckoutConflictException) { return new(OrderResultStatus.PriceChanged); }
        var now = clock.GetUtcNow();
        if (quote.CartId != input.CartId || quote.CartRevision != input.ExpectedCartRevision ||
            quote.CartExpiresAt <= now || !quote.BranchOpen || !quote.Snapshot.IsValid() ||
            Math.Abs((now - quote.Snapshot.PriceCheckedAt).TotalSeconds) > 30 ||
            quote.Snapshot.TotalMinor != input.ExpectedTotalMinor || quote.Snapshot.Delivery != delivery)
        {
            return new(OrderResultStatus.PriceChanged);
        }

        var order = new Order(Guid.NewGuid(), input.CartId, input.ExpectedCartRevision, input.IdempotencyKey,
            scope, actor.CustomerId, actor.CustomerId is null ? GuestHash(actor) : null, fingerprint,
            JsonSerializer.Serialize(quote.Snapshot), now);
        orders.Add(order);
        orders.Add(new CheckoutSaga(order.Id, now.Add(policy.PaymentWindow)));
        Enqueue(order, "ordering.order-created.v1", new { order.Id, order.CustomerId, quote.Snapshot.MenuName }, now);
        Enqueue(order, "ordering.payment-requested.v1", new PaymentRequested(order.Id, order.CustomerId,
            order.GuestKeyHash, quote.Snapshot.TotalMinor, quote.Snapshot.Currency, now.Add(policy.PaymentWindow)), now);
        if (await unitOfWork.TrySaveAsync(ct)) return new(OrderResultStatus.Created, View(order));
        existing = await orders.ByKeyAsync(scope, input.IdempotencyKey, ct)
            ?? await orders.ByCartAsync(input.CartId, input.ExpectedCartRevision, ct);
        return existing is null ? new(OrderResultStatus.Conflict) : await ReplayAsync(actor, existing, fingerprint, ct);
    }

    public async Task<OrderResult> GetAsync(OrderActor actor, Guid id, CancellationToken ct)
    {
        var order = await orders.FindAsync(id, ct);
        if (order is null || !CanRead(actor, order)) return new(OrderResultStatus.Missing);
        if (order.Stage == OrderStage.AwaitingPayment)
        {
            var saga = await orders.SagaAsync(order.Id, ct);
            if (saga!.PaymentDeadline <= clock.GetUtcNow())
            {
                await CancelCoreAsync(order, saga, ct);
                order = await orders.FindAsync(id, ct) ?? order;
            }
        }
        return new(OrderResultStatus.Accepted, View(order));
    }

    public async Task<OrderSummary[]> HistoryAsync(OrderActor actor, int page, CancellationToken ct)
    {
        var scopes = new List<string>();
        if (actor.CustomerId is Guid customer) scopes.Add($"customer:{customer:D}");
        if (GuestHash(actor) is { } hash) scopes.Add($"guest:{hash}");
        var rows = await orders.PageAsync(scopes, page, ct);
        return rows.Select(order =>
        {
            var snapshot = Snapshot(order);
            return new OrderSummary(order.Id, order.Stage.ToString(), order.CreatedAt,
                snapshot.MenuName, snapshot.TotalMinor, snapshot.Currency);
        }).ToArray();
    }

    public async Task<OrderResult> CancelAsync(OrderActor actor, Guid id, CancellationToken ct)
    {
        var order = await orders.FindAsync(id, ct);
        if (order is null || !CanRead(actor, order)) return new(OrderResultStatus.Missing);
        if (order.Stage == OrderStage.Cancelled) return new(OrderResultStatus.Accepted, View(order));
        if (order.Stage is not (OrderStage.AwaitingPayment or OrderStage.Placed)) return new(OrderResultStatus.Conflict);
        var saga = await orders.SagaAsync(id, ct);
        return await CancelCoreAsync(order, saga!, ct)
            ? new(OrderResultStatus.Accepted, View(order)) : new(OrderResultStatus.Conflict);
    }

    public async Task ExpireAsync(CancellationToken ct)
    {
        foreach (var id in await orders.ExpiredAsync(clock.GetUtcNow(), ct))
        {
            var order = await orders.FindAsync(id, ct);
            if (order?.Stage != OrderStage.AwaitingPayment) continue;
            var saga = await orders.SagaAsync(id, ct);
            if (saga!.PaymentDeadline <= clock.GetUtcNow()) await CancelCoreAsync(order, saga, ct);
        }
    }

    private async Task<bool> CancelCoreAsync(Order order, CheckoutSaga saga, CancellationToken ct)
    {
        if (!CancelChanges(order, saga, clock.GetUtcNow())) return false;
        return await unitOfWork.TrySaveAsync(ct);
    }

    private bool CancelChanges(Order order, CheckoutSaga saga, DateTimeOffset now)
    {
        if (!order.MoveTo(OrderStage.Cancelled, now)) return false;
        saga.Cancel();
        Enqueue(order, "ordering.order-cancelled.v1", new { order.Id, order.CustomerId }, now);
        if (saga.PaymentId is Guid payment)
            Enqueue(order, "ordering.refund-requested.v1", new RefundRequested(order.Id, payment), now);
        else Enqueue(order, "ordering.payment-cancelled.v1", new PaymentCancelled(order.Id), now);
        return true;
    }

    public async Task HandleAsync(IntegrationEvent message, CancellationToken ct)
    {
        if (!message.IsValid() || message.Source != "payment" ||
            message.Type is not ("payment.succeeded.v1" or "payment.failed.v1" or "payment.refunded.v1"))
            throw new InvalidIntegrationEventException("unsupported-payment-event");
        var hash = message.ContentHash();
        var existing = await orders.InboxHashAsync(message.MessageId, ct);
        if (existing is not null)
        {
            if (existing != hash) throw new InvalidIntegrationEventException("message-id-collision");
            return;
        }
        var order = await orders.FindAsync(message.AggregateId, ct)
            ?? throw new InvalidIntegrationEventException("unknown-order");
        var saga = await orders.SagaAsync(order.Id, ct)
            ?? throw new InvalidIntegrationEventException("missing-saga");
        var now = clock.GetUtcNow();
        switch (message.Type)
        {
            case "payment.succeeded.v1":
                var paid = message.Data.Deserialize<PaymentSucceeded>(IntegrationEvent.Json)
                    ?? throw new InvalidIntegrationEventException("missing-payment");
                var snapshot = Snapshot(order);
                if (paid.OrderId != order.Id || paid.PaymentId == Guid.Empty ||
                    paid.TotalMinor != snapshot.TotalMinor || paid.Currency != snapshot.Currency)
                    throw new InvalidIntegrationEventException("payment-does-not-match-order");
                if (saga.PaymentConfirmed)
                {
                    if (saga.PaymentId != paid.PaymentId)
                        Enqueue(order, "ordering.refund-requested.v1", new RefundRequested(order.Id, paid.PaymentId), now);
                    break;
                }
                if (order.Stage is not (OrderStage.AwaitingPayment or OrderStage.Cancelled))
                    throw new InvalidIntegrationEventException("invalid-payment-stage");
                if (order.Stage == OrderStage.AwaitingPayment && saga.PaymentDeadline <= now) CancelChanges(order, saga, now);
                saga.ConfirmPayment(paid.PaymentId);
                if (order.Stage == OrderStage.Cancelled)
                    Enqueue(order, "ordering.refund-requested.v1", new RefundRequested(order.Id, paid.PaymentId), now);
                else if (order.MoveTo(OrderStage.Placed, now))
                    Enqueue(order, "ordering.order-placed.v1", new { order.Id, order.CustomerId, snapshot.MenuName }, now);
                break;
            case "payment.failed.v1":
                var failed = message.Data.Deserialize<PaymentFailed>(IntegrationEvent.Json)
                    ?? throw new InvalidIntegrationEventException("missing-payment");
                if (failed.OrderId != order.Id || failed.PaymentId == Guid.Empty)
                    throw new InvalidIntegrationEventException("payment-does-not-match-order");
                if (order.Stage == OrderStage.AwaitingPayment &&
                    (saga.PaymentDeadline <= now || failed.Reason is "expired" or "cancelled"))
                    CancelChanges(order, saga, now);
                break;
            case "payment.refunded.v1":
                var refunded = message.Data.Deserialize<PaymentRefunded>(IntegrationEvent.Json)
                    ?? throw new InvalidIntegrationEventException("missing-refund");
                if (refunded.OrderId != order.Id || refunded.PaymentId == Guid.Empty)
                    throw new InvalidIntegrationEventException("refund-does-not-match-order");
                if (saga.PaymentId == refunded.PaymentId) saga.ConfirmRefund();
                break;
        }
        orders.Add(new InboxReceipt(message.MessageId, message.Source, message.Type, hash, now));
        if (!await unitOfWork.TrySaveAsync(ct) && await orders.InboxHashAsync(message.MessageId, ct) != hash)
            throw new InvalidOperationException("Payment event processing will retry.");
    }

    private async Task<OrderResult> ReplayAsync(OrderActor actor, Order order, string fingerprint, CancellationToken ct)
    {
        if (!CanRead(actor, order)) return new(OrderResultStatus.Missing);
        return order.RequestHash == fingerprint ? await GetAsync(actor, order.Id, ct)
            : new(OrderResultStatus.Conflict, ExistingOrderId: order.Id);
    }

    private void Enqueue(Order order, string type, object payload, DateTimeOffset now) =>
        orders.Add(new OrderOutboxMessage(Guid.NewGuid(), order.Id, type, JsonSerializer.Serialize(payload), now, order.Revision));

    private static string? Scope(OrderActor actor) =>
        actor.CustomerId is Guid subject ? $"customer:{subject:D}" :
            GuestHash(actor) is { } hash ? $"guest:{hash}" : null;

    private static string? GuestHash(OrderActor actor) =>
        actor.GuestKey is { Length: 64 } key && key.All(Uri.IsHexDigit) ? Hash(key) : null;

    private static bool CanRead(OrderActor actor, Order order) =>
        order.CustomerId is Guid customer ? actor.CustomerId == customer :
            GuestHash(actor) is { } hash && order.GuestKeyHash is { } stored &&
                CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(stored));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static OrderSnapshot Snapshot(Order order) => JsonSerializer.Deserialize<OrderSnapshot>(order.SnapshotJson)!;
    private static OrderView View(Order order) =>
        new(order.Id, order.Stage.ToString(), order.Revision, order.CreatedAt, order.UpdatedAt,
            order.CustomerId is null, Snapshot(order));
}
