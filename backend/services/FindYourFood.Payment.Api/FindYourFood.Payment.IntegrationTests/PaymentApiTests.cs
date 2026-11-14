using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FindYourFood.Contracts;
using FindYourFood.Messaging;
using FindYourFood.Payment.Api.Workers;
using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Interfaces;
using FindYourFood.Payment.Application.Services;
using FindYourFood.Payment.Domain;
using FindYourFood.Payment.Infrastructure.Data;
using FindYourFood.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace FindYourFood.Payment.IntegrationTests;

public sealed class PaymentClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class TestGateway : IPaymentGateway
{
    public bool LoseNextResponse { get; set; }
    public ConcurrentDictionary<Guid, GatewayResult> Charges { get; } = new();
    public ConcurrentDictionary<Guid, GatewayResult> Refunds { get; } = new();
    public Task<GatewayResult> ChargeAsync(GatewayCharge charge, CancellationToken ct)
    {
        var result = Charges.GetOrAdd(charge.PaymentId, _ => new(charge.SimulationSuccess, "test-" + charge.PaymentId.ToString("N")));
        if (LoseNextResponse) { LoseNextResponse = false; throw new HttpRequestException("Synthetic lost gateway response."); }
        return Task.FromResult(result);
    }
    public Task<GatewayResult> RefundAsync(GatewayRefund refund, CancellationToken ct) =>
        Task.FromResult(Refunds.GetOrAdd(refund.RefundId, _ => new(true, "test-refund-" + refund.RefundId.ToString("N"))));
}

public sealed class PaymentFixture() : SqlServiceFixture<PaymentHost, PaymentDbContext>("Payment", options => new(options))
{
    public PaymentClock Clock { get; } = new();
    public TestGateway Gateway { get; } = new();
    public string WebhookKeyPath { get; } = Path.Combine(Path.GetTempPath(), $"fyf-webhook-{Guid.NewGuid():N}.key");
    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(Clock);
        services.RemoveAll<IPaymentGateway>();
        services.AddSingleton<IPaymentGateway>(Gateway);
        services.AddScoped<IOrderPaymentAccess, OrderAuthority>();
        var workers = services.Where(row => row.ServiceType == typeof(IHostedService) &&
            row.ImplementationType?.Name == "PaymentRetryWorker").ToArray();
        foreach (var worker in workers) services.Remove(worker);
        services.AddSingleton(new GatewaySignature(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Payment:WebhookKeyPath"] = WebhookKeyPath }).Build(), Clock));
    }
    private sealed class OrderAuthority : IOrderPaymentAccess
    {
        public Task<PayableOrder?> GetAsync(PaymentActor actor, Guid order, CancellationToken ct) =>
            Task.FromResult<PayableOrder?>(new(order, "AwaitingPayment", 15900, "INR"));
    }
}

public sealed class PaymentApiTests(PaymentFixture fixture) : IClassFixture<PaymentFixture>
{
    [Fact]
    public async Task AttemptsArePrivateIdempotentAndDeclinedAttemptsCanRetry()
    {
        var owner = Guid.NewGuid();
        var requested = Request(owner);
        await Handle(requested);
        await Handle(requested);
        using var customer = fixture.Client(owner);
        using var other = fixture.Client(Guid.NewGuid());
        using var staff = fixture.Client(Guid.NewGuid(), new Claim("account_type", "owner"), new Claim("amr", "mfa"));
        var route = Route(requested.AggregateId);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync(route)).StatusCode);
        var failed = new SimulatePayment(Guid.NewGuid(), 15900, false);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync(route + "/simulate", failed with { ExpectedTotalMinor = 1 })).StatusCode);
        Assert.Equal("Failed", (await Read(await customer.PostAsJsonAsync(route + "/simulate", failed))).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync(route + "/simulate", failed with { Success = true })).StatusCode);
        var success = failed with { IdempotencyKey = Guid.NewGuid(), Success = true };
        var replies = await Task.WhenAll(customer.PostAsJsonAsync(route + "/simulate", success),
            customer.PostAsJsonAsync(route + "/simulate", success));
        Assert.All(replies, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var paid = await Read(await customer.PostAsJsonAsync(route + "/simulate", success));
        Assert.Equal("Succeeded", paid.Status);
        Assert.NotNull(paid.PaymentId);
        Assert.Equal(paid.PaymentId, (await Read(await customer.PostAsJsonAsync(route + "/simulate", success))).PaymentId);
        using var scope = fixture.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var intent = await database.Intents.SingleAsync(row => row.OrderId == requested.AggregateId);
        Assert.Equal(2, await database.Set<PaymentAttempt>().CountAsync(row => row.IntentId == intent.Id));
        Assert.Equal(1, await database.Set<OutboxRecord>().CountAsync(row => row.AggregateId == requested.AggregateId && row.Type == "payment.succeeded.v1"));
        Assert.Equal(1, await database.Set<InboxRecord>().CountAsync(row => row.MessageId == requested.MessageId));
    }

    [Fact]
    public async Task LostResponsesReconcileAndRefundsExecuteOnce()
    {
        var owner = Guid.NewGuid();
        var requested = Request(owner);
        await Handle(requested);
        using var customer = fixture.Client(owner);
        var route = Route(requested.AggregateId);
        var input = new SimulatePayment(Guid.NewGuid(), 15900, true);
        fixture.Gateway.LoseNextResponse = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await customer.PostAsJsonAsync(route + "/simulate", input)).StatusCode);
        Assert.Equal("Processing", (await Read(await customer.PostAsJsonAsync(route + "/simulate", input))).Status);
        fixture.Clock.Now = fixture.Clock.Now.AddSeconds(31);
        Guid attempt;
        using (var scope = fixture.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            var intent = await database.Intents.SingleAsync(row => row.OrderId == requested.AggregateId);
            attempt = (await database.Set<PaymentAttempt>().SingleAsync(row => row.IntentId == intent.Id)).Id;
            await scope.ServiceProvider.GetRequiredService<Payments>().ProcessAttemptAsync(attempt, default);
        }
        Assert.Equal("Succeeded", (await customer.GetFromJsonAsync<PaymentView>(route))!.Status);
        var refund = Event(requested.AggregateId, "ordering.refund-requested.v1", new RefundRequested(requested.AggregateId, attempt));
        await Handle(refund);
        await Handle(refund with { MessageId = Guid.NewGuid() });
        await ProcessRefund(attempt);
        await ProcessRefund(attempt);
        Assert.Equal("Refunded", (await customer.GetFromJsonAsync<PaymentView>(route))!.Status);
        using var inspection = fixture.CreateScope();
        var db = inspection.ServiceProvider.GetRequiredService<PaymentDbContext>();
        Assert.Equal(1, await db.Set<RefundInstruction>().CountAsync(row => row.PaymentId == attempt && row.Completed));
        Assert.Equal(1, await db.Set<OutboxRecord>().CountAsync(row => row.AggregateId == requested.AggregateId && row.Type == "payment.refunded.v1"));
        Assert.Single(fixture.Gateway.Charges, row => row.Key == attempt);
    }

    [Fact]
    public async Task OutOfOrderCancellationGuestPrivacyAndWebhookAuthenticationAreEnforced()
    {
        var guest = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(guest)));
        var requested = Request(null, hash);
        await Handle(Event(requested.AggregateId, "ordering.payment-cancelled.v1", new PaymentCancelled(requested.AggregateId)));
        await Handle(requested);
        using var customer = fixture.Client();
        customer.DefaultRequestHeaders.Add("X-Cart-Key", guest);
        Assert.Equal("Cancelled", (await customer.GetFromJsonAsync<PaymentView>(Route(requested.AggregateId)))!.Status);
        using var stranger = fixture.Client();
        stranger.DefaultRequestHeaders.Add("X-Cart-Key", new string('A', 64));
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Route(requested.AggregateId))).StatusCode);

        var owner = Guid.NewGuid();
        requested = Request(owner);
        await Handle(requested);
        using var signedIn = fixture.Client(owner);
        fixture.Gateway.LoseNextResponse = true;
        await signedIn.PostAsJsonAsync(Route(requested.AggregateId) + "/simulate", new SimulatePayment(Guid.NewGuid(), 15900, true));
        Guid payment;
        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            var intent = await db.Intents.SingleAsync(row => row.OrderId == requested.AggregateId);
            payment = (await db.Set<PaymentAttempt>().SingleAsync(row => row.IntentId == intent.Id)).Id;
        }
        var key = RandomNumberGenerator.GetBytes(32);
        await File.WriteAllBytesAsync(fixture.WebhookKeyPath, key);
        try
        {
            var completion = new GatewayCompletion(Guid.NewGuid(), payment, 15900, "INR", true, "signed-confirmation");
            var body = JsonSerializer.SerializeToUtf8Bytes(completion, IntegrationEvent.Json);
            var timestamp = fixture.Clock.Now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var anonymous = fixture.Client();
            async Task<HttpResponseMessage> Webhook(string signature, byte[] payload)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhooks/development")
                { Content = new ByteArrayContent(payload) };
                request.Content.Headers.ContentType = new("application/json");
                request.Headers.Add("X-Payment-Timestamp", timestamp);
                request.Headers.Add("X-Payment-Signature", signature);
                return await anonymous.SendAsync(request);
            }
            var valid = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(timestamp + ".").Concat(body).ToArray()));
            Assert.Equal(HttpStatusCode.Unauthorized, (await Webhook(new string('0', 64), body)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Webhook(valid, body)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Webhook(valid, body)).StatusCode);
            var conflicting = JsonSerializer.SerializeToUtf8Bytes(completion with { EventId = Guid.NewGuid(), Success = false }, IntegrationEvent.Json);
            var conflictingSignature = Convert.ToHexString(HMACSHA256.HashData(key,
                Encoding.UTF8.GetBytes(timestamp + ".").Concat(conflicting).ToArray()));
            Assert.Equal(HttpStatusCode.BadRequest, (await Webhook(conflictingSignature, conflicting)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Webhook(valid, body.Concat(new byte[] { 32 }).ToArray())).StatusCode);
            Assert.Equal("Succeeded", (await signedIn.GetFromJsonAsync<PaymentView>(Route(requested.AggregateId)))!.Status);
            fixture.Clock.Now = fixture.Clock.Now.AddMinutes(6);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Webhook(valid, body)).StatusCode);
        }
        finally { File.Delete(fixture.WebhookKeyPath); }
    }

    private static string Route(Guid order) => "/api/v1/payments/orders/" + order;
    [Fact]
    public async Task DeclinedGuestAttemptThenRefundUsesTheSuccessfulAttempt()
    {
        var guest = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(guest)));
        var requested = Request(null, hash);
        await Handle(requested);
        using var client = fixture.Client();
        client.DefaultRequestHeaders.Add("X-Cart-Key", guest);
        var route = Route(requested.AggregateId);
        var declined = new SimulatePayment(Guid.NewGuid(), 15900, false);
        Assert.Equal("Failed", (await Read(await client.PostAsJsonAsync(route + "/simulate", declined))).Status);
        var paid = await Read(await client.PostAsJsonAsync(route + "/simulate", declined with { IdempotencyKey = Guid.NewGuid(), Success = true }));
        Assert.Equal("Succeeded", paid.Status);
        await Handle(Event(requested.AggregateId, "ordering.refund-requested.v1", new RefundRequested(requested.AggregateId, paid.PaymentId!.Value)));
        await ProcessRefund(paid.PaymentId.Value);
        Assert.Equal("Refunded", (await client.GetFromJsonAsync<PaymentView>(route))!.Status);
        using var scope = fixture.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var intent = await database.Intents.SingleAsync(row => row.OrderId == requested.AggregateId);
        Assert.Equal(2, await database.Set<PaymentAttempt>().CountAsync(row => row.IntentId == intent.Id));
        Assert.Equal(paid.PaymentId, (await database.Set<RefundInstruction>().SingleAsync(row => row.OrderId == requested.AggregateId)).PaymentId);
    }

    private static async Task<PaymentView> Read(HttpResponseMessage response)
    { response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<PaymentView>())!; }
    private IntegrationEvent Request(Guid? owner, string? hash = null) =>
        Event(Guid.NewGuid(), "ordering.payment-requested.v1", new PaymentRequested(
            Guid.Empty, owner, hash, 15900, "INR", fixture.Clock.Now.AddMinutes(15)), fixOrder: true);
    private IntegrationEvent Event(Guid order, string type, object data, bool fixOrder = false) =>
        new(Guid.NewGuid(), "ordering", type, order, 1, fixture.Clock.Now,
            JsonSerializer.SerializeToElement(fixOrder ? ((PaymentRequested)data) with { OrderId = order } : data, IntegrationEvent.Json));
    private async Task Handle(IntegrationEvent message)
    {
        using var scope = fixture.CreateScope();
        await scope.ServiceProvider.GetRequiredService<Payments>().HandleAsync(message, default);
    }
    private async Task ProcessRefund(Guid payment)
    {
        using var scope = fixture.CreateScope();
        await scope.ServiceProvider.GetRequiredService<Payments>().ProcessRefundAsync(payment, default);
    }
}
