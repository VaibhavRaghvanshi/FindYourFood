using FindYourFood.Ordering.Application.DTOs;

namespace FindYourFood.Ordering.Application.Interfaces;

public interface ICartCheckout
{
    Task<CheckoutQuote> QuoteAsync(OrderActor actor, CheckoutRequest input, CancellationToken ct);
}
