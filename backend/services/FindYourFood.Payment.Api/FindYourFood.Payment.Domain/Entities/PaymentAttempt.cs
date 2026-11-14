namespace FindYourFood.Payment.Domain;

public enum AttemptState { Pending, Succeeded, Failed }

public sealed class PaymentAttempt(Guid id, Guid intentId, Guid idempotencyKey, bool simulationSuccess, DateTimeOffset createdAt)
{
    public Guid Id { get; private set; } = id;
    public Guid IntentId { get; private set; } = intentId;
    public Guid IdempotencyKey { get; private set; } = idempotencyKey;
    public bool SimulationSuccess { get; private set; } = simulationSuccess;
    public DateTimeOffset CreatedAt { get; private set; } = createdAt;
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ProviderReference { get; private set; }
    public AttemptState State { get; private set; }
    public long Revision { get; private set; } = 1;

    public DateTimeOffset? NextRetryAt { get; private set; }
    public int Attempts { get; private set; }

    public bool Claim(DateTimeOffset now)
    {
        if (State != AttemptState.Pending || NextRetryAt > now) return false;
        NextRetryAt = now.AddSeconds(30);
        Attempts++;
        Revision++;
        return true;
    }

    public bool Complete(bool success, string reference, DateTimeOffset now)
    {
        if (State != AttemptState.Pending) return false;
        State = success ? AttemptState.Succeeded : AttemptState.Failed;
        ProviderReference = reference;
        CompletedAt = now;
        Revision++;
        return true;
    }
}

public sealed class RefundInstruction(Guid id, Guid paymentId, Guid orderId, long totalMinor, string currency)
{
    public Guid Id { get; private set; } = id;
    public Guid PaymentId { get; private set; } = paymentId;
    public Guid OrderId { get; private set; } = orderId;
    public long TotalMinor { get; private set; } = totalMinor;
    public string Currency { get; private set; } = currency;
    public bool Completed { get; private set; }
    public string? ProviderReference { get; private set; }
    public long Revision { get; private set; } = 1;

    public DateTimeOffset? NextRetryAt { get; private set; }
    public int Attempts { get; private set; }

    public bool Claim(DateTimeOffset now)
    {
        if (Completed || NextRetryAt > now) return false;
        NextRetryAt = now.AddSeconds(30);
        Attempts++;
        Revision++;
        return true;
    }

    public void Complete(string reference)
    {
        if (Completed) return;
        Completed = true;
        ProviderReference = reference;
        Revision++;
    }
}

public sealed class CancelledOrder(Guid orderId, DateTimeOffset occurredAt)
{
    public Guid OrderId { get; private set; } = orderId;
    public DateTimeOffset OccurredAt { get; private set; } = occurredAt;
}
