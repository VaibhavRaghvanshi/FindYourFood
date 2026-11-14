using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Interfaces;

namespace FindYourFood.Payment.Infrastructure.Gateways;

// The server owns the simulated transaction ID and stable replay result; no card details are accepted.
public sealed class DevelopmentPaymentGateway : IPaymentGateway
{
    public Task<GatewayResult> ChargeAsync(GatewayCharge charge, CancellationToken ct) =>
        Task.FromResult(new GatewayResult(charge.SimulationSuccess, $"sim-charge-{charge.PaymentId:N}"));
    public Task<GatewayResult> RefundAsync(GatewayRefund refund, CancellationToken ct) =>
        Task.FromResult(new GatewayResult(true, $"sim-refund-{refund.RefundId:N}"));
}

public sealed class UnconfiguredPaymentGateway : IPaymentGateway
{
    public Task<GatewayResult> ChargeAsync(GatewayCharge charge, CancellationToken ct) =>
        throw new InvalidOperationException("Configure a production payment gateway.");
    public Task<GatewayResult> RefundAsync(GatewayRefund refund, CancellationToken ct) =>
        throw new InvalidOperationException("Configure a production payment gateway.");
}
