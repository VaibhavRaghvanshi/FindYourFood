using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FindYourFood.Cart.Application.DTOs;
using FindYourFood.Cart.Application.Interfaces;
using FindYourFood.Cart.Domain;

namespace FindYourFood.Cart.Infrastructure.Http;

public sealed class CommerceSource(HttpClient provider, HttpClient catalog, HttpClient location, TimeProvider clock) : ICommerceSource
{
    private sealed record OpeningWindow(int Day, string Opens, string Closes);
    private sealed record BranchDetails(string TimeZone, OpeningWindow[] Hours, DateTimeOffset? ClosedUntil,
        double? Latitude, double? Longitude);
    private sealed record Branch(BranchDetails Details);

    public async Task<BranchCatalog?> BranchAsync(Guid id, CancellationToken ct)
    {
        using var branchResponse = await provider.GetAsync($"/api/v1/providers/public/branches/{id:D}", ct);
        if (branchResponse.StatusCode == HttpStatusCode.NotFound) return null;
        branchResponse.EnsureSuccessStatusCode();
        using var menuResponse = await catalog.GetAsync($"/api/v1/catalog/branches/{id:D}", ct);
        if (menuResponse.StatusCode == HttpStatusCode.NotFound) return null;
        menuResponse.EnsureSuccessStatusCode();
        var branch = (await branchResponse.Content.ReadFromJsonAsync<Branch>(ct))?.Details
            ?? throw new HttpRequestException("Missing public branch.");
        var menu = await menuResponse.Content.ReadFromJsonAsync<PublishedMenu>(ct)
            ?? throw new HttpRequestException("Missing published menu.");
        if (menu.BranchId != id || menu.Version < 1) throw new HttpRequestException("Invalid published menu.");
        var pickup = branch.Latitude is double latitude && branch.Longitude is double longitude
            ? new GeoPoint(latitude, longitude) : null;
        return new(menu, pickup, IsOpen(branch));
    }

    public async Task<DeliveryQuote> QuoteAsync(GeoPoint origin, GeoPoint destination, CancellationToken ct)
    {
        using var response = await location.PostAsJsonAsync("/api/v1/location/route-quote", new { origin, destination }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeliveryQuote>(ct)
            ?? throw new HttpRequestException("Missing delivery quote.");
    }

    private bool IsOpen(BranchDetails branch)
    {
        var now = clock.GetUtcNow();
        if (branch.ClosedUntil > now || !TimeZoneInfo.TryFindSystemTimeZoneById(branch.TimeZone, out var zone)) return false;
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var day = ((int)local.DayOfWeek + 6) % 7 + 1;
        var time = TimeOnly.FromDateTime(local.DateTime);
        return branch.Hours.Any(window => window.Day == day &&
            TimeOnly.TryParseExact(window.Opens, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var opens) &&
            TimeOnly.TryParseExact(window.Closes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closes) &&
            time >= opens && time < closes);
    }
}
