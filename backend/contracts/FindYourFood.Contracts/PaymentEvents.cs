namespace FindYourFood.Contracts;

public sealed record PaymentRequested(Guid OrderId, Guid? CustomerId, string? GuestKeyHash, long TotalMinor,
    string Currency, DateTimeOffset PaymentDeadline);
public sealed record PaymentSucceeded(Guid OrderId, Guid PaymentId, long TotalMinor, string Currency);
public sealed record PaymentFailed(Guid OrderId, Guid PaymentId, string Reason);
public sealed record RefundRequested(Guid OrderId, Guid PaymentId);
public sealed record PaymentRefunded(Guid OrderId, Guid PaymentId);
public sealed record PaymentCancelled(Guid OrderId);
