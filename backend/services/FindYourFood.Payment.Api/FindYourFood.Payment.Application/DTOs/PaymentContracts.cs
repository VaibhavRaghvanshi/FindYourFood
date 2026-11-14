namespace FindYourFood.Payment.Application.DTOs;

public sealed record PaymentActor(Guid? CustomerId, string? GuestKey, string? Authorization);
public sealed record SimulatePayment(Guid IdempotencyKey, long ExpectedTotalMinor, bool Success);
public sealed record PaymentView(Guid Id, Guid OrderId, string Status, long TotalMinor, string Currency,
    Guid? PaymentId, DateTimeOffset Deadline);
public sealed record PayableOrder(Guid Id, string Status, long TotalMinor, string Currency);
public sealed record GatewayCharge(Guid PaymentId, long TotalMinor, string Currency, bool SimulationSuccess);
public sealed record GatewayRefund(Guid RefundId, Guid PaymentId, long TotalMinor, string Currency, string ProviderReference);
public sealed record GatewayResult(bool Success, string Reference);
public sealed record GatewayCompletion(Guid EventId, Guid PaymentId, long TotalMinor, string Currency, bool Success, string Reference);
public enum PaymentResultStatus { Accepted, Processing, Invalid, Missing, Conflict }
public sealed record PaymentResult(PaymentResultStatus Status, PaymentView? Payment = null);

public sealed record PendingPaymentWork(IReadOnlyList<Guid> Payments, IReadOnlyList<Guid> Refunds);
