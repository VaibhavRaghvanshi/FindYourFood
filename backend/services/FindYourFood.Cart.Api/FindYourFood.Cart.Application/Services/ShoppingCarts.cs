using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FindYourFood.Cart.Application.DTOs;
using FindYourFood.Cart.Application.Interfaces;
using FindYourFood.Cart.Domain;

namespace FindYourFood.Cart.Application.Services;

public sealed class ShoppingCarts(ICartRepository carts, ICommerceSource commerce, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<CartResult> CreateAsync(CartActor actor, Guid branch, CancellationToken ct)
    {
        if (branch == Guid.Empty) return new(CartStatus.Invalid);
        var source = await commerce.BranchAsync(branch, ct);
        if (source is null) return new(CartStatus.ReviewRequired);
        var key = actor.CustomerId is null ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) : null;
        var cart = new ShoppingCart(Guid.NewGuid(), branch, actor.CustomerId, key is null ? null : Hash(key),
            clock.GetUtcNow().AddHours(24));
        carts.Add(cart);
        return await unitOfWork.TrySaveAsync(ct)
            ? new(CartStatus.Accepted, View(cart, source), GuestKey: key) : new(CartStatus.Conflict);
    }

    public async Task<CartResult> ReadAsync(CartActor actor, Guid id, CancellationToken ct)
    {
        var cart = await FindAsync(actor, id, ct);
        if (cart is null) return new(CartStatus.Missing);
        var source = await commerce.BranchAsync(cart.BranchId, ct);
        return source is null ? new(CartStatus.ReviewRequired) : new(CartStatus.Accepted, View(cart, source));
    }

    public async Task<CartResult> ReadMineAsync(CartActor actor, CancellationToken ct)
    {
        if (actor.CustomerId is not Guid customer) return new(CartStatus.Missing);
        var cart = await carts.FindMineAsync(customer, clock.GetUtcNow(), ct);
        return cart is null ? new(CartStatus.Missing) : await ReadAsync(actor, cart.Id, ct);
    }

    public async Task<CartResult> RemoveAsync(CartActor actor, Guid id, CancellationToken ct)
    {
        var cart = await FindAsync(actor, id, ct);
        if (cart is null) return new(CartStatus.Missing);
        carts.Remove(cart);
        return new(await unitOfWork.TrySaveAsync(ct) ? CartStatus.Accepted : CartStatus.Conflict);
    }

    public async Task<CartResult> ChangeAsync(CartActor actor, Guid id, ChangeCart input, CancellationToken ct)
    {
        if (input.ExpectedRevision < 1 || !CartSelection.Valid(input.Lines)) return new(CartStatus.Invalid);
        var cart = await FindAsync(actor, id, ct);
        if (cart is null) return new(CartStatus.Missing);
        if (cart.Revision != input.ExpectedRevision) return new(CartStatus.Conflict);
        var source = await commerce.BranchAsync(cart.BranchId, ct);
        if (source is null || CartPricing.Calculate(source.Menu, input.Lines).SubtotalMinor is null)
        {
            return new(CartStatus.ReviewRequired);
        }

        cart.Replace(JsonSerializer.Serialize(input.Lines));
        return await unitOfWork.TrySaveAsync(ct)
            ? new(CartStatus.Accepted, View(cart, source)) : new(CartStatus.Conflict);
    }

    public async Task<CartResult> QuoteAsync(CartActor actor, Guid id, QuoteRequest input, CancellationToken ct)
    {
        if (input.ExpectedRevision < 1 || input.Destination?.IsValid != true) return new(CartStatus.Invalid);
        var cart = await FindAsync(actor, id, ct);
        if (cart is null) return new(CartStatus.Missing);
        if (cart.Revision != input.ExpectedRevision) return new(CartStatus.Conflict);
        var source = await commerce.BranchAsync(cart.BranchId, ct);
        if (source is null || !source.OpenNow || source.Pickup is null) return new(CartStatus.ReviewRequired);
        var view = View(cart, source);
        if (view.Selections.Count == 0 || view.Prices.SubtotalMinor is null) return new(CartStatus.ReviewRequired);
        var delivery = await commerce.QuoteAsync(source.Pickup, input.Destination, ct);
        if (!delivery.Serviceable || delivery.ZoneId is null || delivery.DeliveryFeeMinor is not >= 0)
        {
            return new(CartStatus.ReviewRequired);
        }

        var quote = new CartQuote(view, input.Destination, delivery.ZoneId.Value, delivery.DeliveryFeeMinor.Value,
            view.Prices.SubtotalMinor.Value + delivery.DeliveryFeeMinor.Value, clock.GetUtcNow());
        return new(CartStatus.Accepted, view, quote);
    }

    private async Task<ShoppingCart?> FindAsync(CartActor actor, Guid id, CancellationToken ct)
    {
        var cart = await carts.FindAsync(id, ct);
        if (cart is null || cart.ExpiresAt <= clock.GetUtcNow()) return null;
        if (cart.CustomerId is Guid customer) return actor.CustomerId == customer ? cart : null;
        if (actor.GuestKey is not { Length: 64 } key || cart.GuestKeyHash is null) return null;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cart.GuestKeyHash), Encoding.UTF8.GetBytes(Hash(key)))
            ? cart : null;
    }

    private static CartView View(ShoppingCart cart, BranchCatalog source)
    {
        var selections = JsonSerializer.Deserialize<CartSelection[]>(cart.SelectionsJson)!;
        return new(cart.Id, cart.BranchId, cart.Revision, cart.ExpiresAt, source.Menu.Currency, source.Menu.Version,
            source.OpenNow, selections, CartPricing.Calculate(source.Menu, selections));
    }

    private static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
