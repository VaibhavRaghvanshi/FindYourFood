namespace FindYourFood.Ordering.Domain;

public enum CheckoutState { WaitingForPayment, WaitingForProvider, WaitingForDelivery, Completed, Cancelled, Compensating }

public sealed class CheckoutSaga(Guid orderId, DateTimeOffset paymentDeadline)
{
    public Guid OrderId { get; private set; } = orderId;
    public DateTimeOffset PaymentDeadline { get; private set; } = paymentDeadline;
    public CheckoutState State { get; private set; } = CheckoutState.WaitingForPayment;
    public bool PaymentConfirmed { get; private set; }
    public Guid? PaymentId { get; private set; }
    public long Revision { get; private set; } = 1;

    public void Cancel()
    {
        State = PaymentConfirmed ? CheckoutState.Compensating : CheckoutState.Cancelled;
        Revision++;
    }

    public bool ConfirmPayment(Guid paymentId)
    {
        if (PaymentConfirmed) return false;
        PaymentConfirmed = true;
        PaymentId = paymentId;
        State = State == CheckoutState.Cancelled ? CheckoutState.Compensating : CheckoutState.WaitingForProvider;
        Revision++;
        return true;
    }

    public void ConfirmRefund()
    {
        if (!PaymentConfirmed || State != CheckoutState.Compensating) return;
        State = CheckoutState.Cancelled;
        Revision++;
    }

    public void AwaitDelivery()
    {
        State = CheckoutState.WaitingForDelivery;
        Revision++;
    }

    public void Complete()
    {
        State = CheckoutState.Completed;
        Revision++;
    }
}
