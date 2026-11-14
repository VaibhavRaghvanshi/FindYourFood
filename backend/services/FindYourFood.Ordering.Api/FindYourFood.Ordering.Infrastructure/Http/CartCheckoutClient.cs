using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Domain;

namespace FindYourFood.Ordering.Infrastructure.Http;

public sealed class CartCheckoutClient(HttpClient client) : ICartCheckout
{
    private sealed record Point(double Latitude, double Longitude);
    private sealed record Selection(Guid LineId, Guid ItemId, int Quantity, Guid? VariantId, Guid[] AddOnIds);
    private sealed record PriceLine(Guid LineId, Guid ItemId, string Name, int Quantity, string? Variant,
        string[] AddOns, long UnitPriceMinor, long TotalMinor, string? Error);
    private sealed record Prices(PriceLine[] Lines, long? SubtotalMinor);
    private sealed record Cart(Guid Id, Guid BranchId, string MenuName, long Revision, DateTimeOffset ExpiresAt,
        string Currency, long MenuVersion, bool BranchOpen, Selection[] Selections, Prices Prices);
    private sealed record Quote(Cart Cart, Point Destination, Guid ZoneId, long DeliveryFeeMinor, long TotalMinor,
        DateTimeOffset CheckedAt);

    public async Task<CheckoutQuote> QuoteAsync(OrderActor actor, CheckoutRequest input, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carts/{input.CartId:D}/quote")
        {
            Content = JsonContent.Create(new
            {
                expectedRevision = input.ExpectedCartRevision,
                destination = new Point(input.Delivery.Latitude, input.Delivery.Longitude)
            })
        };
        if (AuthenticationHeaderValue.TryParse(actor.Authorization, out var authorization))
        {
            request.Headers.Authorization = authorization;
        }
        if (actor.GuestKey is { Length: 64 } key) request.Headers.Add("X-Cart-Key", key);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new CheckoutMissingException();
        }
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest) throw new CheckoutConflictException();
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<Quote>(ct)
            ?? throw new HttpRequestException("Missing cart quote.");
        var cart = quote.Cart;
        if (cart?.Prices?.SubtotalMinor is not long subtotal || cart.Prices.Lines is null || cart.Selections is null ||
            cart.Prices.Lines.Length != cart.Selections.Length || cart.Prices.Lines.Any(line => line.Error is not null) ||
            cart.Prices.Lines.Select(line => line.LineId).Distinct().Count() != cart.Prices.Lines.Length ||
            quote.Destination is null || quote.Destination.Latitude != input.Delivery.Latitude ||
            quote.Destination.Longitude != input.Delivery.Longitude)
        {
            throw new CheckoutConflictException();
        }

        var prices = cart.Prices.Lines.ToDictionary(line => line.LineId);
        var items = new List<OrderItem>();
        foreach (var selection in cart.Selections)
        {
            if (!prices.TryGetValue(selection.LineId, out var line) ||
                selection.ItemId != line.ItemId || selection.Quantity != line.Quantity)
            {
                throw new HttpRequestException("Cart lines do not match their prices.");
            }
            items.Add(new(selection.LineId, selection.ItemId, line.Name, selection.Quantity, selection.VariantId,
                line.Variant, selection.AddOnIds, line.AddOns, line.UnitPriceMinor, line.TotalMinor));
        }
        var snapshot = new OrderSnapshot(cart.BranchId, cart.MenuName, cart.MenuVersion, cart.Currency, items,
            subtotal, quote.DeliveryFeeMinor, quote.TotalMinor, quote.ZoneId, input.Delivery, quote.CheckedAt);
        return new(cart.Id, cart.Revision, cart.ExpiresAt, cart.BranchOpen, snapshot);
    }
}
