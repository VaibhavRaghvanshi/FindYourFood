using FindYourFood.Payment.Application.DTOs;

namespace FindYourFood.Payment.Application.Interfaces;

public interface IOrderPaymentAccess
{
    Task<PayableOrder?> GetAsync(PaymentActor actor, Guid order, CancellationToken ct);
}
