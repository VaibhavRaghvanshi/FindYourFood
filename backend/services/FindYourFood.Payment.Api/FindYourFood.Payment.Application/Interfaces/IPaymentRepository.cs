using FindYourFood.Contracts;
using FindYourFood.Payment.Domain;

namespace FindYourFood.Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentIntent?> OrderAsync(Guid order, CancellationToken ct);
    Task<PaymentIntent?> IntentAsync(Guid id, CancellationToken ct);
    Task<PaymentAttempt?> AttemptAsync(Guid payment, CancellationToken ct);
    Task<PaymentAttempt?> ByKeyAsync(Guid intent, Guid key, CancellationToken ct);
    Task<bool> WasCancelledAsync(Guid order, CancellationToken ct);
    Task<RefundInstruction?> RefundAsync(Guid payment, CancellationToken ct);
    Task<IReadOnlyList<Guid>> PendingAttemptsAsync(DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<Guid>> PendingRefundsAsync(DateTimeOffset now, CancellationToken ct);
    Task<string?> InboxHashAsync(Guid id, CancellationToken ct);
    void Add(PaymentIntent intent);
    void Add(PaymentAttempt attempt);
    void Add(RefundInstruction refund);
    void Add(CancelledOrder cancellation);
    void Add(InboxReceipt receipt);
    void Add(IntegrationEvent message);
}
