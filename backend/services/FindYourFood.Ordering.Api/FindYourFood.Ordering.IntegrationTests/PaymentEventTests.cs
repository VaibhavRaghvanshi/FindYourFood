using System.Net.Http.Json;
using System.Text.Json;
using FindYourFood.Contracts;
using FindYourFood.Messaging;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Services;
using FindYourFood.Ordering.Domain;
using FindYourFood.Ordering.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FindYourFood.Ordering.IntegrationTests;

public sealed class PaymentEventTests(OrderingFixture fixture) : IClassFixture<OrderingFixture>
{
    [Fact]
    public async Task InboxSuppressesReplaysAndLateSuccessRequestsCompensation()
    {
        using var client = fixture.Client(Guid.NewGuid());
        var delivery = new DeliveryContact("Sample", "+919999999999", "Synthetic street", "Pune", "411001", 18.52, 73.85);
        var input = new CheckoutRequest(Guid.NewGuid(), Guid.NewGuid(), 2, 15900, delivery);
        var order = (await (await client.PostAsJsonAsync("/api/v1/orders", input)).Content.ReadFromJsonAsync<OrderView>())!;
        var payment = Guid.NewGuid();
        var paid = Event(order.Id, new PaymentSucceeded(order.Id, payment, 15900, "INR"));
        await Handle(paid);
        await Handle(paid);
        await Handle(paid with { MessageId = Guid.NewGuid() });
        Assert.Equal("Placed", (await client.GetFromJsonAsync<OrderView>("/api/v1/orders/" + order.Id))!.Status);
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            Assert.Equal(2, await database.Set<InboxRecord>().CountAsync());
            Assert.Equal(1, await database.Outbox.CountAsync(row => row.Type == "ordering.order-placed.v1"));
        }

        var lateInput = input with { IdempotencyKey = Guid.NewGuid(), CartId = Guid.NewGuid() };
        var late = (await (await client.PostAsJsonAsync("/api/v1/orders", lateInput)).Content.ReadFromJsonAsync<OrderView>())!;
        await client.PostAsync("/api/v1/orders/" + late.Id + "/cancel", null);
        await Handle(Event(late.Id, new PaymentSucceeded(late.Id, Guid.NewGuid(), 15900, "INR")));
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            Assert.Equal(CheckoutState.Compensating, (await database.Sagas.SingleAsync(row => row.OrderId == late.Id)).State);
            Assert.Equal(1, await database.Outbox.CountAsync(row => row.AggregateId == late.Id && row.Type == "ordering.refund-requested.v1"));
        }
        var wrongAmount = Event(order.Id, new PaymentSucceeded(order.Id, payment, 1, "INR"));
        await Assert.ThrowsAsync<InvalidIntegrationEventException>(() => Handle(wrongAmount));
    }

    [Fact]
    public async Task OutboxLeasesPreventConcurrentClaimsAndStaleCompletion()
    {
        using var client = fixture.Client(Guid.NewGuid());
        var input = new CheckoutRequest(Guid.NewGuid(), Guid.NewGuid(), 2, 15900,
            new("Sample", "+919999999999", "Synthetic street", "Pune", "411001", 18.52, 73.85));
        await client.PostAsJsonAsync("/api/v1/orders", input);
        using var firstScope = fixture.CreateScope();
        using var secondScope = fixture.CreateScope();
        var first = new SqlMessageStore(firstScope.ServiceProvider.GetRequiredService<OrderingDbContext>());
        var second = new SqlMessageStore(secondScope.ServiceProvider.GetRequiredService<OrderingDbContext>());
        var firstLease = Guid.NewGuid();
        var secondLease = Guid.NewGuid();
        var claimed = await Task.WhenAll(first.ClaimAsync(firstLease, fixture.Clock.Now, default),
            second.ClaimAsync(secondLease, fixture.Clock.Now, default));
        Assert.All(claimed, Assert.NotNull);
        Assert.NotEqual(claimed[0]!.Id, claimed[1]!.Id);
        await first.CompleteAsync(claimed[0]!.Id, secondLease, fixture.Clock.Now, default);
        var before = await firstScope.ServiceProvider.GetRequiredService<OrderingDbContext>()
            .Outbox.AsNoTracking().SingleAsync(row => row.Id == claimed[0]!.Id);
        Assert.Null(before.PublishedAt);
        await first.CompleteAsync(claimed[0]!.Id, firstLease, fixture.Clock.Now, default);
        using var inspection = fixture.CreateScope();
        var database = inspection.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.NotNull((await database.Outbox.SingleAsync(row => row.Id == claimed[0]!.Id)).PublishedAt);
        Assert.Null((await database.Outbox.SingleAsync(row => row.Id == claimed[1]!.Id)).PublishedAt);
    }

    private async Task Handle(IntegrationEvent message)
    {
        using var scope = fixture.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrderCheckout>().HandleAsync(message, default);
    }

    private IntegrationEvent Event(Guid order, PaymentSucceeded data) =>
        new(Guid.NewGuid(), "payment", "payment.succeeded.v1", order, 1, fixture.Clock.Now,
            JsonSerializer.SerializeToElement(data, IntegrationEvent.Json));
}
