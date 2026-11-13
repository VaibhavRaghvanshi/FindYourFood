using System.Net;
using System.Net.Http.Json;
using FindYourFood.Discovery.Application.DTOs;
using FindYourFood.Discovery.Domain;
using FindYourFood.Discovery.Infrastructure.Data;
using FindYourFood.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FindYourFood.Discovery.IntegrationTests;

public sealed class SearchClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 9, 5, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class SearchFixture() : SqlServiceFixture<DiscoveryHost, DiscoveryDbContext>("Discovery", options => new(options))
{
    public SearchClock Clock { get; } = new();
    public bool Outage { get; set; }
    public string Food { get; set; } = "Tomato soup";

    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(Clock);
        foreach (var service in new[] { "provider", "catalog", "location" })
        {
            services.AddHttpClient(service).ConfigurePrimaryHttpMessageHandler(() => new DirectoryHandler(this, service));
        }
    }

    private sealed class DirectoryHandler(SearchFixture fixture, string service) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (fixture.Outage) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var branch = new PublicBranch(Guid.Parse("0f211072-d0c8-4694-8943-965cd107c204"), "Soup Kitchen", "restaurant",
                new("Central", "Synthetic street", "Pune", "Asia/Kolkata", [new(1, "09:00", "17:00")], null, 18.52, 73.85));
            object content = service switch
            {
                "provider" => new[] { branch },
                "catalog" => new { version = 1, menu = new { name = "Lunch menu", items = new[] { new { name = fixture.Food, available = true, tags = new[] { "Vegetarian" } } } } },
                _ => new[] { new { serviceable = true, deliveryFeeMinor = 3900, distanceKm = 1.5 } }
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(content) });
        }
    }
}

public sealed class SearchApiTests(SearchFixture fixture) : IClassFixture<SearchFixture>
{
    [Fact]
    public async Task PublishedProjectionRefreshIsAtomicAndFiltersUseCurrentHours()
    {
        using var client = fixture.Client();
        const string route = "/api/v1/discovery/search";
        var request = new SearchRequest("Pune", "soup", "Vegetarian", true, 1, 18.52, 73.85);
        var response = await client.PostAsJsonAsync(route, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = (await response.Content.ReadFromJsonAsync<SearchResults>())!;
        var hit = Assert.Single(results.Items);
        Assert.True(hit.OpenNow);
        Assert.True(hit.Serviceable);
        Assert.Equal(3900, hit.DeliveryFeeMinor);

        var noMatch = await client.PostAsJsonAsync(route, request with { Query = "pizza" });
        Assert.Empty((await noMatch.Content.ReadFromJsonAsync<SearchResults>())!.Items);
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(2);
        fixture.Outage = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync(route, request)).StatusCode);
        fixture.Outage = false;
        fixture.Food = "Pumpkin soup";
        var refreshed = await client.PostAsJsonAsync(route, request);
        var current = Assert.Single((await refreshed.Content.ReadFromJsonAsync<SearchResults>())!.Items);
        Assert.Equal("Pumpkin soup", Assert.Single(current.Foods));
    }
}
