using FindYourFood.Cart.Domain;

namespace FindYourFood.Cart.Application.DTOs;

public sealed record CartActor(Guid? CustomerId, string? GuestKey);
public sealed record CreateCart(Guid BranchId);
public sealed record ChangeCart(long ExpectedRevision, IReadOnlyList<CartSelection> Lines);
public sealed record GeoPoint(double Latitude, double Longitude)
{
    public bool IsValid => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;
}
public sealed record QuoteRequest(long ExpectedRevision, GeoPoint Destination);
public sealed record CartView(Guid Id, Guid BranchId, long Revision, DateTimeOffset ExpiresAt, string Currency,
    long MenuVersion, bool BranchOpen, IReadOnlyList<CartSelection> Selections, CartPrices Prices);
public sealed record CreatedCart(CartView Cart, string? GuestKey);
public sealed record DeliveryQuote(bool Serviceable, Guid? ZoneId, int? DeliveryFeeMinor, double? DistanceKm);
public sealed record CartQuote(CartView Cart, GeoPoint Destination, Guid ZoneId, long DeliveryFeeMinor,
    long TotalMinor, DateTimeOffset CheckedAt);
public sealed record BranchCatalog(PublishedMenu Menu, GeoPoint? Pickup, bool OpenNow);
public enum CartStatus { Accepted, Missing, Invalid, Conflict, ReviewRequired }
public sealed record CartResult(CartStatus Status, CartView? Cart = null, CartQuote? Quote = null, string? GuestKey = null);
