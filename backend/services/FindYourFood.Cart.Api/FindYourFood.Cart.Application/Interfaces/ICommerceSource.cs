using FindYourFood.Cart.Application.DTOs;

namespace FindYourFood.Cart.Application.Interfaces;

public interface ICommerceSource
{
    Task<BranchCatalog?> BranchAsync(Guid branch, CancellationToken ct);
    Task<DeliveryQuote> QuoteAsync(GeoPoint origin, GeoPoint destination, CancellationToken ct);
}
