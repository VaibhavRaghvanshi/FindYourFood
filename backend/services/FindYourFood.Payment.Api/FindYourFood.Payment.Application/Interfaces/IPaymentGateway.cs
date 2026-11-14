using FindYourFood.Payment.Application.DTOs;

namespace FindYourFood.Payment.Application.Interfaces;

public interface IPaymentGateway
{
    Task<GatewayResult> ChargeAsync(GatewayCharge charge, CancellationToken ct);
    Task<GatewayResult> RefundAsync(GatewayRefund refund, CancellationToken ct);
}
