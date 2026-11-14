using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Interfaces;

namespace FindYourFood.Payment.Infrastructure.Http;

public sealed class OrderPaymentClient(HttpClient client) : IOrderPaymentAccess
{
    private sealed record Order(Guid Id, string Status, Snapshot Snapshot);
    private sealed record Snapshot(long TotalMinor, string Currency);
    public async Task<PayableOrder?> GetAsync(PaymentActor actor, Guid order, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/{order:D}");
        if (AuthenticationHeaderValue.TryParse(actor.Authorization, out var authorization))
            request.Headers.Authorization = authorization;
        if (actor.GuestKey is { Length: 64 } key) request.Headers.Add("X-Cart-Key", key);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return null;
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<Order>(ct)
            ?? throw new HttpRequestException("Missing order authority.");
        if (value.Id != order || value.Snapshot is null) throw new HttpRequestException("Invalid order authority.");
        return new(value.Id, value.Status, value.Snapshot.TotalMinor, value.Snapshot.Currency);
    }
}
