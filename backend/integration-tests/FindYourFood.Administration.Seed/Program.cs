using System.Text.Json;
using FindYourFood.Experience.Infrastructure.Data;
using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Application.Services;
using FindYourFood.Identity.Infrastructure;
using FindYourFood.Identity.Infrastructure.Persistence;
using FindYourFood.Organization.Infrastructure;
using FindYourFood.Provider.Infrastructure.Data;
using FindYourFood.Catalog.Infrastructure.Data;
using FindYourFood.Customer.Infrastructure.Data;
using FindYourFood.Location.Infrastructure.Data;
using FindYourFood.Discovery.Infrastructure.Data;
using FindYourFood.Cart.Infrastructure.Data;
using FindYourFood.Ordering.Infrastructure.Data;
using FindYourFood.Payment.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FindYourFood.Messaging;
using FindYourFood.Ordering.Domain;
using RabbitMQ.Client;
using OtpNet;

var identity = IsolatedConnection("ConnectionStrings__Identity", "FindYourFood_IdentityTest_");
var organization = IsolatedConnection("ConnectionStrings__Organization", "FindYourFood_OrganizationTest_");
var experience = args.Contains("--experience", StringComparer.Ordinal)
    ? IsolatedConnection("ConnectionStrings__Experience", "FindYourFood_ExperienceTest_")
    : null;
var providerConnection = args.Contains("--provider", StringComparer.Ordinal)
    ? IsolatedConnection("ConnectionStrings__Provider", "FindYourFood_ProviderTest_")
    : null;
var output = Required("FINDYOURFOOD_E2E_ACCOUNTS");
var marketplace = args.Contains("--marketplace", StringComparer.Ordinal);
var services = new ServiceCollection();
services.AddLogging();
var clock = new EnrollmentClock(DateTimeOffset.UtcNow.AddSeconds(-30));
services.AddSingleton<TimeProvider>(clock);
if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("The administration browser test uses Windows DPAPI.");
}

services.AddDataProtection().SetApplicationName("FindYourFood.Identity")
    .PersistKeysToFileSystem(new DirectoryInfo(Required("Identity__KeyRingPath"))).ProtectKeysWithDpapi();
services.AddIdentityPersistence(identity);
services.AddDbContext<OrganizationDbContext>(options => options.UseSqlServer(organization));
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var identityDatabase = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
var organizationDatabase = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
await using var experienceDatabase = experience is null ? null : new ExperienceDbContext(
    new DbContextOptionsBuilder<ExperienceDbContext>().UseSqlServer(experience).Options);
await using var providerDatabase = providerConnection is null ? null : new ProviderDbContext(
    new DbContextOptionsBuilder<ProviderDbContext>().UseSqlServer(providerConnection).Options);
await using var catalogDatabase = marketplace ? TestDatabase<CatalogDbContext>("Catalog", options => new(options)) : null;
await using var customerDatabase = marketplace ? TestDatabase<CustomerDbContext>("Customer", options => new(options)) : null;
await using var locationDatabase = marketplace ? TestDatabase<LocationDbContext>("Location", options => new(options)) : null;
await using var discoveryDatabase = marketplace ? TestDatabase<DiscoveryDbContext>("Discovery", options => new(options)) : null;
await using var cartDatabase = marketplace ? TestDatabase<CartDbContext>("Cart", options => new(options)) : null;
await using var orderingDatabase = marketplace ? TestDatabase<OrderingDbContext>("Ordering", options => new(options)) : null;
await using var paymentDatabase = args.Contains("--payments", StringComparer.Ordinal)
    ? TestDatabase<PaymentDbContext>("Payment", options => new(options)) : null;
DbContext?[] marketplaceDatabases = [catalogDatabase, customerDatabase, locationDatabase, discoveryDatabase, cartDatabase, orderingDatabase, paymentDatabase];

if (args.Any(argument => argument is not ("--cleanup" or "--experience" or "--provider" or "--marketplace" or "--payments" or "--verify-payments" or "--diagnose-payments")))
{
    throw new ArgumentException("Use --experience or --provider to include a service, and --cleanup to remove isolated databases.");
}

if (args.Contains("--cleanup", StringComparer.Ordinal))
{
    if (paymentDatabase is not null)
    {
        var catalog = new SqlConnectionStringBuilder(IsolatedConnection("ConnectionStrings__Payment", "FindYourFood_PaymentTest_")).InitialCatalog;
        var prefix = "findyourfood.test." + catalog["FindYourFood_PaymentTest_".Length..];
        var factory = new ConnectionFactory { HostName = "localhost", UserName = "findyourfood", VirtualHost = "findyourfood",
            Password = File.ReadAllText(Required("Messaging__PasswordPath")).Trim() };
        await using var broker = await factory.CreateConnectionAsync();
        await using var channel = await broker.CreateChannelAsync();
        foreach (var service in new[] { "ordering", "payment" })
        {
            await channel.QueueDeclareAsync(prefix + "." + service, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeleteAsync(prefix + "." + service, ifUnused: false, ifEmpty: false);
        }
        await channel.ExchangeDeclareAsync(prefix + ".events", ExchangeType.Topic, durable: true, autoDelete: false);
        await channel.ExchangeDeleteAsync(prefix + ".events", ifUnused: false);
    }
    await identityDatabase.Database.EnsureDeletedAsync();
    await organizationDatabase.Database.EnsureDeletedAsync();
    if (experienceDatabase is not null)
    {
        await experienceDatabase.Database.EnsureDeletedAsync();
    }
    if (providerDatabase is not null)
    {
        await providerDatabase.Database.EnsureDeletedAsync();
    }
    File.Delete(output);
    foreach (var database in marketplaceDatabases)
    {
        if (database is not null) await database.Database.EnsureDeletedAsync();
    }
    return;
}

if (args.Contains("--diagnose-payments", StringComparer.Ordinal))
{
    if (paymentDatabase is null || orderingDatabase is null) throw new InvalidOperationException("Payments profile required.");
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        orders = await orderingDatabase.Orders.AsNoTracking().GroupBy(row => row.Stage).Select(group => new { stage = group.Key, count = group.Count() }).ToArrayAsync(),
        sagas = await orderingDatabase.Sagas.AsNoTracking().GroupBy(row => row.State).Select(group => new { state = group.Key, count = group.Count() }).ToArrayAsync(),
        payments = await paymentDatabase.Intents.AsNoTracking().GroupBy(row => row.State).Select(group => new { state = group.Key, count = group.Count() }).ToArrayAsync(),
        refundJobs = await paymentDatabase.Set<FindYourFood.Payment.Domain.RefundInstruction>().AsNoTracking().Select(row => new { row.Completed, row.Attempts, row.NextRetryAt }).ToArrayAsync(),
        orderingOutbox = await orderingDatabase.Set<OutboxRecord>().AsNoTracking().Select(row => new { row.Type, row.PublishedAt, row.FailedAt, row.Attempts, row.LastError }).ToArrayAsync(),
        paymentOutbox = await paymentDatabase.Set<OutboxRecord>().AsNoTracking().Select(row => new { row.Type, row.PublishedAt, row.FailedAt, row.Attempts, row.LastError }).ToArrayAsync(),
        rejected = await paymentDatabase.Set<RejectedMessage>().AsNoTracking().Select(row => row.Reason).ToArrayAsync()
    }));
    return;
}

if (args.Contains("--verify-payments", StringComparer.Ordinal))
{
    if (paymentDatabase is null || orderingDatabase is null) throw new InvalidOperationException("Payments profile required.");
    var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
    do
    {
        var settled = await orderingDatabase.Sagas.AsNoTracking().CountAsync(row => row.State == CheckoutState.Cancelled && row.PaymentConfirmed);
        var confirmations = await paymentDatabase.Set<OutboxRecord>().CountAsync(row => row.PublishedAt != null && row.Type == "payment.succeeded.v1");
        var refunds = await paymentDatabase.Set<OutboxRecord>().CountAsync(row => row.PublishedAt != null && row.Type == "payment.refunded.v1");
        if (settled == 2 && confirmations == 2 && refunds == 2)
        {
            if (await paymentDatabase.Set<RejectedMessage>().AnyAsync() || await orderingDatabase.Set<RejectedMessage>().AnyAsync())
                throw new InvalidOperationException("Unexpected rejected broker messages.");
            Console.WriteLine("Verified two broker-confirmed payments and two completed refund sagas.");
            return;
        }
        await Task.Delay(500);
    } while (DateTimeOffset.UtcNow < deadline);
    throw new InvalidOperationException("Payment/refund broker verification timed out.");
}

await identityDatabase.Database.MigrateAsync();
foreach (var database in marketplaceDatabases)
{
    if (database is not null) await database.Database.MigrateAsync();
}
await organizationDatabase.Database.MigrateAsync();
if (experienceDatabase is not null)
{
    await experienceDatabase.Database.MigrateAsync();
}
if (providerDatabase is not null)
{
    await providerDatabase.Database.MigrateAsync();
}
var accounts = scope.ServiceProvider.GetRequiredService<PrivilegedAccounts>();
var enrollment = scope.ServiceProvider.GetRequiredService<IAuthenticatorEnrollment>();
var owner = await EnrollAsync("owner", isOwner: true);
var manager = await EnrollAsync("manager", isOwner: false);
var employee = await EnrollAsync("employee", isOwner: false);
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { owner, manager, employee }));

async Task<object> EnrollAsync(string name, bool isOwner)
{
    var email = $"{name}-{Guid.NewGuid():N}@example.test";
    var password = Guid.NewGuid().ToString("N");
    var secret = enrollment.CreateSecret();
    var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(clock.GetUtcNow().UtcDateTime);
    var account = await accounts.EnrollAsync(email, password, secret, code, isOwner)
        ?? throw new InvalidOperationException("Synthetic test enrollment failed.");
    return new { id = account.Id, email, password, secret };
}

static string Required(string name) => Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Set {name}.");

static TDatabase TestDatabase<TDatabase>(string service, Func<DbContextOptions<TDatabase>, TDatabase> create)
    where TDatabase : DbContext =>
    create(new DbContextOptionsBuilder<TDatabase>().UseSqlServer(
        IsolatedConnection($"ConnectionStrings__{service}", $"FindYourFood_{service}Test_")).Options);

static string IsolatedConnection(string name, string prefix)
{
    var value = Required(name);
    var catalog = new SqlConnectionStringBuilder(value).InitialCatalog;
    if (!catalog.StartsWith(prefix, StringComparison.Ordinal) ||
        !Guid.TryParseExact(catalog[prefix.Length..], "N", out _))
    {
        throw new InvalidOperationException($"{name} must use a generated test-only catalog.");
    }

    return value;
}

file sealed class EnrollmentClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
