using FindYourFood.Ordering.Domain;

namespace FindYourFood.Ordering.Application.DTOs;

public sealed record OrderActor(Guid? CustomerId, string? GuestKey, string? Authorization);
public sealed record CheckoutRequest(Guid IdempotencyKey, Guid CartId, long ExpectedCartRevision,
    long ExpectedTotalMinor, DeliveryContact Delivery);
public sealed record CheckoutQuote(Guid CartId, long CartRevision, DateTimeOffset CartExpiresAt, bool BranchOpen,
    OrderSnapshot Snapshot);
public sealed record OrderView(Guid Id, string Status, long Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, bool GuestOrder, OrderSnapshot Snapshot);
public sealed record OrderSummary(Guid Id, string Status, DateTimeOffset CreatedAt, string MenuName,
    long TotalMinor, string Currency);
public enum OrderResultStatus { Created, Accepted, Invalid, Missing, Conflict, PriceChanged }
public sealed record OrderResult(OrderResultStatus Status, OrderView? Order = null, Guid? ExistingOrderId = null);
public sealed class CheckoutConflictException : Exception;
public sealed class CheckoutMissingException : Exception;
