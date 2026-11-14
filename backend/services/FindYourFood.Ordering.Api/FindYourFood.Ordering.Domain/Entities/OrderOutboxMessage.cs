namespace FindYourFood.Ordering.Domain;

public sealed class OrderOutboxMessage(Guid id, Guid orderId, string type, string payloadJson, DateTimeOffset occurredAt)
{
    public Guid Id { get; private set; } = id;
    public Guid OrderId { get; private set; } = orderId;
    public string Type { get; private set; } = type;
    public string PayloadJson { get; private set; } = payloadJson;
    public DateTimeOffset OccurredAt { get; private set; } = occurredAt;
    public DateTimeOffset? PublishedAt { get; private set; }
}
