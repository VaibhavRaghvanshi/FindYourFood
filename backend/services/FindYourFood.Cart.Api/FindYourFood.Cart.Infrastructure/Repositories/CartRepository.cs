using FindYourFood.Cart.Application.Interfaces;
using FindYourFood.Cart.Domain;
using FindYourFood.Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Cart.Infrastructure.Repositories;

internal sealed class CartRepository(CartDbContext database) : ICartRepository
{
    public Task<ShoppingCart?> FindAsync(Guid id, CancellationToken ct) =>
        database.Carts.SingleOrDefaultAsync(cart => cart.Id == id, ct);

    public Task<ShoppingCart?> FindMineAsync(Guid customer, DateTimeOffset now, CancellationToken ct) =>
        database.Carts.Where(cart => cart.CustomerId == customer && cart.ExpiresAt > now)
            .OrderByDescending(cart => cart.ExpiresAt).ThenBy(cart => cart.Id).FirstOrDefaultAsync(ct);

    public void Add(ShoppingCart cart) => database.Carts.Add(cart);
    public void Remove(ShoppingCart cart) => database.Carts.Remove(cart);
}
