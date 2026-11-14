using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Domain;

namespace FindYourFood.Ordering.Application.Services;

public sealed record CheckoutPolicy(TimeSpan PaymentWindow);

public sealed class OrderCheckout(IOrderRepository orders, ICartCheckout carts, IUnitOfWork unitOfWork,
    TimeProvider clock, CheckoutPolicy policy)
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
        Enqueue(order, "ordering.payment-requested.v1", new
        {
            order.Id, order.CustomerId, order.GuestKeyHash, quote.Snapshot.TotalMinor, quote.Snapshot.Currency
        }, now);
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
        var now = clock.GetUtcNow();
        if (!order.MoveTo(OrderStage.Cancelled, now)) return false;
        saga.Cancel();
        Enqueue(order, "ordering.order-cancelled.v1", new { order.Id, order.CustomerId }, now);
        Enqueue(order, saga.PaymentConfirmed ? "ordering.refund-requested.v1" : "ordering.payment-cancelled.v1",
            new { order.Id }, now);
        return await unitOfWork.TrySaveAsync(ct);
    }

    private async Task<OrderResult> ReplayAsync(OrderActor actor, Order order, string fingerprint, CancellationToken ct)
    {
        if (!CanRead(actor, order)) return new(OrderResultStatus.Missing);
        return order.RequestHash == fingerprint ? await GetAsync(actor, order.Id, ct)
            : new(OrderResultStatus.Conflict, ExistingOrderId: order.Id);
    }

    private void Enqueue(Order order, string type, object payload, DateTimeOffset now) =>
        orders.Add(new OrderOutboxMessage(Guid.NewGuid(), order.Id, type, JsonSerializer.Serialize(payload), now));

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
