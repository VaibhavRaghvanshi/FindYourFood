namespace FindYourFood.Ordering.Domain;

public enum OrderStage { AwaitingPayment, Placed, Accepted, Preparing, ReadyForPickup, OutForDelivery, Delivered, Cancelled }

public sealed class Order(Guid id, Guid cartId, long cartRevision, Guid idempotencyKey, string ownerScope,
    Guid? customerId, string? guestKeyHash, string requestHash, string snapshotJson, DateTimeOffset createdAt)
{
    public Guid Id { get; private set; } = id;
    public Guid CartId { get; private set; } = cartId;
    public long CartRevision { get; private set; } = cartRevision;
    public Guid IdempotencyKey { get; private set; } = idempotencyKey;
    public string OwnerScope { get; private set; } = ownerScope;
    public Guid? CustomerId { get; private set; } = customerId;
    public string? GuestKeyHash { get; private set; } = guestKeyHash;
    public string RequestHash { get; private set; } = requestHash;
    public string SnapshotJson { get; private set; } = snapshotJson;
    public OrderStage Stage { get; private set; } = OrderStage.AwaitingPayment;
    public long Revision { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; } = createdAt;
    public DateTimeOffset UpdatedAt { get; private set; } = createdAt;

    public bool MoveTo(OrderStage next, DateTimeOffset now)
    {
        var allowed = (Stage, next) switch
        {
            (OrderStage.AwaitingPayment, OrderStage.Placed or OrderStage.Cancelled) => true,
            (OrderStage.Placed, OrderStage.Accepted or OrderStage.Cancelled) => true,
            (OrderStage.Accepted, OrderStage.Preparing) => true,
            (OrderStage.Preparing, OrderStage.ReadyForPickup) => true,
            (OrderStage.ReadyForPickup, OrderStage.OutForDelivery) => true,
            (OrderStage.OutForDelivery, OrderStage.Delivered) => true,
            _ => false
        };
        if (!allowed) return false;
        Stage = next;
        UpdatedAt = now;
        Revision++;
        return true;
    }
}
