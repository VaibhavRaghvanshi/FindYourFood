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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
DbContext?[] marketplaceDatabases = [catalogDatabase, customerDatabase, locationDatabase, discoveryDatabase, cartDatabase, orderingDatabase];

if (args.Any(argument => argument is not ("--cleanup" or "--experience" or "--provider" or "--marketplace")))
{
    throw new ArgumentException("Use --experience or --provider to include a service, and --cleanup to remove isolated databases.");
}

if (args.Contains("--cleanup", StringComparer.Ordinal))
{
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
