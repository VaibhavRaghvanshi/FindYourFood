using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FindYourFood.Cart.Application.DTOs;
using FindYourFood.Cart.Domain;
using FindYourFood.Cart.Infrastructure.Data;
using FindYourFood.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FindYourFood.Cart.IntegrationTests;

public sealed class CartClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class CartFixture() : SqlServiceFixture<CartHost, CartDbContext>("Cart", options => new(options))
{
    public Guid BranchId { get; } = Guid.NewGuid();
    public Guid ItemId { get; } = Guid.NewGuid();
    public Guid ZoneId { get; } = Guid.NewGuid();
    public int PriceMinor { get; set; } = 12000;
    public bool Covered { get; set; } = true;
    public CartClock Clock { get; } = new();

    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(Clock);
        foreach (var service in new[] { "provider", "catalog", "location" })
        {
            services.AddHttpClient(service).ConfigurePrimaryHttpMessageHandler(() => new CommerceHandler(this, service));
        }
    }

    private sealed class CommerceHandler(CartFixture fixture, string service) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            object content = service switch
            {
                "provider" => new
                {
                    details = new { timeZone = "UTC", hours = Enumerable.Range(1, 7).Select(day => new { day, opens = "00:00", closes = "23:59" }),
                        closedUntil = (DateTimeOffset?)null, latitude = 18.52, longitude = 73.85 }
                },
                "catalog" => new PublishedMenu(fixture.BranchId, 1, "INR",
                    new("Lunch", [new(fixture.ItemId, "Soup", fixture.PriceMinor, true, [], [])])),
                _ => new DeliveryQuote(fixture.Covered, fixture.ZoneId, 3900, 1.5)
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(content) });
        }
    }
}

public sealed class CartApiTests(CartFixture fixture) : IClassFixture<CartFixture>
{
    [Fact]
    public async Task GuestCapabilityRevisionAndAuthoritativeQuoteProtectTheCart()
    {
        const string root = "/api/v1/carts";
        using var guest = fixture.Client();
        using var stranger = fixture.Client();
        using var staff = fixture.Client(Guid.NewGuid(), new Claim("account_type", "staff"));
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync(root, new CreateCart(fixture.BranchId))).StatusCode);
        var createdResponse = await guest.PostAsJsonAsync(root, new CreateCart(fixture.BranchId));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = (await createdResponse.Content.ReadFromJsonAsync<CreatedCart>())!;
        Assert.Equal(64, created.GuestKey!.Length);
        guest.DefaultRequestHeaders.Add("X-Cart-Key", created.GuestKey);
        var route = root + "/" + created.Cart.Id;
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(route)).StatusCode);
        var line = new CartSelection(Guid.NewGuid(), fixture.ItemId, 2, null, []);
        var changed = await guest.PutAsJsonAsync(route, new ChangeCart(created.Cart.Revision, [line]));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var saved = (await changed.Content.ReadFromJsonAsync<CartView>())!;
        Assert.Equal(24000, saved.Prices.SubtotalMinor);
        var writes = await Task.WhenAll(guest.PutAsJsonAsync(route, new ChangeCart(saved.Revision, [line with { Quantity = 3 }])),
            guest.PutAsJsonAsync(route, new ChangeCart(saved.Revision, [line with { Quantity = 4 }])));
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.Conflict);
        saved = (await guest.GetFromJsonAsync<CartView>(route))!;
        fixture.PriceMinor = 15000;
        var quoteResponse = await guest.PostAsJsonAsync(route + "/quote", new QuoteRequest(saved.Revision, new(18.52, 73.85)));
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quote = (await quoteResponse.Content.ReadFromJsonAsync<CartQuote>())!;
        Assert.Equal(saved.Selections[0].Quantity * 15000 + 3900, quote.TotalMinor);
        Assert.DoesNotContain(created.GuestKey, await (await guest.GetAsync(route)).Content.ReadAsStringAsync());
        fixture.Covered = false;
        Assert.Equal(HttpStatusCode.Conflict,
            (await guest.PostAsJsonAsync(route + "/quote", new QuoteRequest(saved.Revision, new(18.52, 73.85)))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await guest.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync(route)).StatusCode);

        using var customer = fixture.Client(Guid.NewGuid());
        using var other = fixture.Client(Guid.NewGuid());
        var owned = (await (await customer.PostAsJsonAsync(root, new CreateCart(fixture.BranchId)))
            .Content.ReadFromJsonAsync<CreatedCart>())!;
        Assert.Null(owned.GuestKey);
        Assert.Equal(owned.Cart.Id, (await customer.GetFromJsonAsync<CartView>(root + "/mine"))!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + "/" + owned.Cart.Id)).StatusCode);
        fixture.Clock.Now = fixture.Clock.Now.AddHours(24);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync(root + "/" + owned.Cart.Id)).StatusCode);
    }
}
