using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Domain;
using FindYourFood.Ordering.Infrastructure.Data;
using FindYourFood.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FindYourFood.Ordering.IntegrationTests;

public sealed class OrderingClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class OrderingFixture() : SqlServiceFixture<OrderingHost, OrderingDbContext>("Ordering", options => new(options))
{
    public OrderingClock Clock { get; } = new();
    public long TotalMinor { get; set; } = 15900;
    public bool Outage { get; set; }

    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(Clock);
        services.AddScoped<ICartCheckout>(_ => new CartQuoteSource(this));
    }

    private sealed class CartQuoteSource(OrderingFixture fixture) : ICartCheckout
    {
        public Task<CheckoutQuote> QuoteAsync(OrderActor actor, CheckoutRequest input, CancellationToken ct)
        {
            if (fixture.Outage) throw new HttpRequestException("Synthetic cart outage.");
            var itemsMinor = fixture.TotalMinor - 3900;
            var item = new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Soup", 1, null, null, [], [], itemsMinor, itemsMinor);
            var snapshot = new OrderSnapshot(Guid.NewGuid(), "Lunch", 1, "INR", [item], itemsMinor, 3900,
                fixture.TotalMinor, Guid.NewGuid(), input.Delivery, fixture.Clock.Now);
            return Task.FromResult(new CheckoutQuote(input.CartId, input.ExpectedCartRevision,
                fixture.Clock.Now.AddHours(24), true, snapshot));
        }
    }
}

public sealed class CheckoutApiTests(OrderingFixture fixture) : IClassFixture<OrderingFixture>
{
    [Fact]
    public async Task CheckoutReplayPrivacyOutboxAndCancellationSurviveRetries()
    {
        const string root = "/api/v1/orders";
        using var customer = fixture.Client(Guid.NewGuid());
        using var other = fixture.Client(Guid.NewGuid());
        using var staff = fixture.Client(Guid.NewGuid(), new Claim("account_type", "owner"), new Claim("amr", "mfa"));
        var delivery = new DeliveryContact("Sample", "+919999999999", "Synthetic street", "Pune", "411001", 18.52, 73.85);
        var input = new CheckoutRequest(Guid.NewGuid(), Guid.NewGuid(), 2, 15900, delivery);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync(root, input)).StatusCode);
        var submits = await Task.WhenAll(customer.PostAsJsonAsync(root, input), customer.PostAsJsonAsync(root, input));
        Assert.Single(submits, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(submits, response => response.StatusCode == HttpStatusCode.OK);
        var order = (await submits[0].Content.ReadFromJsonAsync<OrderView>())!;
        var route = root + "/" + order.Id;
        Assert.Equal("AwaitingPayment", order.Status);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<OrderSummary[]>(root))!);
        Assert.Equal(order.Id, Assert.Single((await customer.GetFromJsonAsync<OrderSummary[]>(root))!).Id);
        Assert.Equal(HttpStatusCode.Conflict,
            (await customer.PostAsJsonAsync(root, input with { ExpectedTotalMinor = 16900 })).StatusCode);
        fixture.Outage = true;
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync(root, input)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync(root, input with { IdempotencyKey = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await customer.PostAsJsonAsync(root, input with { IdempotencyKey = Guid.NewGuid(), CartId = Guid.NewGuid() })).StatusCode);
        fixture.Outage = false;
        fixture.TotalMinor = 16900;
        Assert.Equal(15900, (await customer.GetFromJsonAsync<OrderView>(route))!.Snapshot.TotalMinor);
        Assert.Equal(HttpStatusCode.Conflict,
            (await customer.PostAsJsonAsync(root, input with { IdempotencyKey = Guid.NewGuid(), CartId = Guid.NewGuid() })).StatusCode);
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            Assert.Equal(2, await database.Outbox.CountAsync(message => message.AggregateId == order.Id));
            Assert.Single(await database.Sagas.ToListAsync());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync(route + "/cancel", null)).StatusCode);
        var cancelled = await customer.PostAsync(route + "/cancel", null);
        Assert.Equal("Cancelled", (await cancelled.Content.ReadFromJsonAsync<OrderView>())!.Status);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsync(route + "/cancel", null)).StatusCode);
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            Assert.Equal(4, await database.Outbox.CountAsync(message => message.AggregateId == order.Id));
        }

        var expiringInput = input with { CartId = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid(), ExpectedTotalMinor = 16900 };
        var expiring = (await (await customer.PostAsJsonAsync(root, expiringInput)).Content.ReadFromJsonAsync<OrderView>())!;
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(16);
        Assert.Equal("Cancelled", (await customer.GetFromJsonAsync<OrderView>(root + "/" + expiring.Id))!.Status);

        using var guest = fixture.Client();
        var guestKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        guest.DefaultRequestHeaders.Add("X-Cart-Key", guestKey);
        var guestInput = expiringInput with { CartId = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid() };
        var guestOrder = (await (await guest.PostAsJsonAsync(root, guestInput)).Content.ReadFromJsonAsync<OrderView>())!;
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync(root + "/" + guestOrder.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync(root + "/" + guestOrder.Id)).StatusCode);
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            var stored = await database.Orders.SingleAsync(row => row.Id == guestOrder.Id);
            Assert.NotEqual(guestKey, stored.GuestKeyHash);
            Assert.DoesNotContain(guestKey, stored.SnapshotJson);
        }
    }
}
