using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FindYourFood.Location.Application.DTOs;
using FindYourFood.Location.Domain;
using FindYourFood.Location.Infrastructure.Data;
using FindYourFood.Testing;
using Xunit;

namespace FindYourFood.Location.IntegrationTests;

public sealed class LocationFixture() : SqlServiceFixture<LocationHost, LocationDbContext>("Location", options => new(options));

public sealed class LocationApiTests(LocationFixture fixture) : IClassFixture<LocationFixture>
{
    [Fact]
    public async Task OwnerMaintainsAreasAndOnlyActiveZonesMatch()
    {
        using var customer = fixture.Client();
        using var staff = fixture.Client(Guid.NewGuid(), new Claim("account_type", "staff"), new Claim("amr", "mfa"));
        using var owner = fixture.Client(Guid.NewGuid(), new Claim("account_type", "owner"), new Claim("amr", "mfa"));
        const string serviceability = "/api/v1/location/serviceability";
        var id = Guid.NewGuid();
        var route = $"/api/v1/location/zones/{id:D}";
        var point = new Coordinates(18.52, 73.85);
        var settings = new ZoneSettings("Central", "Pune", point, 7, 3900, true);
        var zone = new ZoneUpdate(0, settings);

        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync(route, zone)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(route, zone)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(route, zone)).StatusCode);
        var match = (await (await customer.PostAsJsonAsync(serviceability, point)).Content
            .ReadFromJsonAsync<Serviceability>())!;
        Assert.True(match.Serviceable);
        Assert.Equal(3900, match.DeliveryFeeMinor);
        Assert.Equal(id, match.ZoneId);

        const string quoteRoute = "/api/v1/location/route-quote";
        var quote = (await (await customer.PostAsJsonAsync(quoteRoute, new DeliveryQuoteRequest(point, point)))
            .Content.ReadFromJsonAsync<Serviceability>())!;
        Assert.True(quote.Serviceable);
        var farAway = new Coordinates(28.61, 77.21);
        var outside = (await (await customer.PostAsJsonAsync(quoteRoute, new DeliveryQuoteRequest(farAway, point)))
            .Content.ReadFromJsonAsync<Serviceability>())!;
        Assert.False(outside.Serviceable);
        var matrix = (await (await customer.PostAsJsonAsync("/api/v1/location/coverage-matrix",
            new CoverageMatrixRequest(point, [point, farAway]))).Content.ReadFromJsonAsync<Serviceability[]>())!;
        Assert.True(matrix[0].Serviceable);
        Assert.False(matrix[1].Serviceable);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(route,
            new ZoneUpdate(1, settings with { Enabled = false }))).StatusCode);
        var disabled = (await (await customer.PostAsJsonAsync(serviceability, point)).Content
            .ReadFromJsonAsync<Serviceability>())!;
        Assert.False(disabled.Serviceable);
    }
}
