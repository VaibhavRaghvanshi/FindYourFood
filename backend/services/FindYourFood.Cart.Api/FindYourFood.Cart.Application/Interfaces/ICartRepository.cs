using FindYourFood.Cart.Domain;

namespace FindYourFood.Cart.Application.Interfaces;

public interface ICartRepository
{
    Task<ShoppingCart?> FindAsync(Guid id, CancellationToken ct);
    Task<ShoppingCart?> FindMineAsync(Guid customer, DateTimeOffset now, CancellationToken ct);
    void Add(ShoppingCart cart);
    void Remove(ShoppingCart cart);
}
