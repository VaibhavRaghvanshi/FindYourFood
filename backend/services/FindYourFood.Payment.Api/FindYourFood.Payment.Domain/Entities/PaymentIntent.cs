namespace FindYourFood.Payment.Domain;

public enum PaymentState { Pending, Processing, Failed, Succeeded, Cancelled, RefundPending, Refunded }

public sealed class PaymentIntent(Guid id, Guid orderId, Guid? customerId, string? guestKeyHash,
    long totalMinor, string currency, DateTimeOffset deadline, DateTimeOffset createdAt)
{
    public Guid Id { get; private set; } = id;
    public Guid OrderId { get; private set; } = orderId;
    public Guid? CustomerId { get; private set; } = customerId;
    public string? GuestKeyHash { get; private set; } = guestKeyHash;
    public long TotalMinor { get; private set; } = totalMinor;
    public string Currency { get; private set; } = currency;
    public DateTimeOffset Deadline { get; private set; } = deadline;
    public DateTimeOffset CreatedAt { get; private set; } = createdAt;
    public long Revision { get; private set; } = 1;
    public PaymentState State { get; private set; }
    public Guid? ActiveAttemptId { get; private set; }
    public Guid? SuccessfulPaymentId { get; private set; }
    public bool CancellationRequested { get; private set; }

    public bool Begin(Guid attempt)
    {
        if (State is not (PaymentState.Pending or PaymentState.Failed) || CancellationRequested) return false;
        State = PaymentState.Processing;
        ActiveAttemptId = attempt;
        Revision++;
        return true;
    }

    public void Complete(Guid payment, bool succeeded)
    {
        if (ActiveAttemptId != payment) return;
        ActiveAttemptId = null;
        if (succeeded)
        {
            SuccessfulPaymentId = payment;
            State = CancellationRequested ? PaymentState.RefundPending : PaymentState.Succeeded;
        }
        else State = CancellationRequested ? PaymentState.Cancelled : PaymentState.Failed;
        Revision++;
    }

    public void Cancel()
    {
        if (CancellationRequested) return;
        CancellationRequested = true;
        if (State is PaymentState.Pending or PaymentState.Failed) State = PaymentState.Cancelled;
        if (State == PaymentState.Succeeded) State = PaymentState.RefundPending;
        Revision++;
    }

    public void Refunded(Guid payment)
    {
        if (SuccessfulPaymentId != payment || State == PaymentState.Refunded) return;
        State = PaymentState.Refunded;
        Revision++;
    }
}
