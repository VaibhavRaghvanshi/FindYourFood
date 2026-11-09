using System.Text.Json;
using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Application.Services;
using FindYourFood.Identity.Infrastructure;
using FindYourFood.Identity.Infrastructure.Persistence;
using FindYourFood.Organization.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

var identity = IsolatedConnection("ConnectionStrings__Identity", "FindYourFood_IdentityTest_");
var organization = IsolatedConnection("ConnectionStrings__Organization", "FindYourFood_OrganizationTest_");
var output = Required("FINDYOURFOOD_E2E_ACCOUNTS");
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

if (args is ["--cleanup"])
{
    await identityDatabase.Database.EnsureDeletedAsync();
    await organizationDatabase.Database.EnsureDeletedAsync();
    File.Delete(output);
    return;
}

if (args.Length != 0)
{
    throw new ArgumentException("Use no arguments to seed, or --cleanup to remove the isolated databases.");
}

await identityDatabase.Database.MigrateAsync();
await organizationDatabase.Database.MigrateAsync();
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
