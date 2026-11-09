using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using FindYourFood.Organization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FindYourFood.Organization.IntegrationTests;

public sealed class OrganizationSqlFixture : IAsyncLifetime
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"fyf-org-{Guid.NewGuid():N}.pem");
    private readonly Dictionary<string, string?> _previous = [];
    private DbContextOptions<OrganizationDbContext>? _database;
    private WebApplicationFactory<OrganizationHost>? _factory;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FINDYOURFOOD_TEST_SQL")
            ?? throw new InvalidOperationException("Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.");
        var isolated = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = $"FindYourFood_OrganizationTest_{Guid.NewGuid():N}"
        };
        _database = new DbContextOptionsBuilder<OrganizationDbContext>().UseSqlServer(isolated.ConnectionString).Options;
        await using var db = CreateDatabase();
        await db.Database.MigrateAsync();

        await File.WriteAllTextAsync(_keyPath, _key.ExportSubjectPublicKeyInfoPem());
        Set("DOTNET_ENVIRONMENT", "Development");
        Set("ASPNETCORE_ENVIRONMENT", "Development");
        Set("ConnectionStrings__Organization", isolated.ConnectionString);
        Set("Tokens__PublicKeyPath", _keyPath);
        _factory = new WebApplicationFactory<OrganizationHost>();
    }

    public HttpClient Client(Guid? subject = null, string? accountType = null, bool mfa = true)
    {
        var client = _factory!.CreateClient();
        if (subject is Guid id)
        {
            var claims = new List<Claim> { new("sub", id.ToString("D")) };
            if (accountType is not null) claims.Add(new("account_type", accountType));
            if (mfa) claims.Add(new("amr", "mfa"));

            var key = new RsaSecurityKey(_key.ExportParameters(true));
            var jwt = new JwtSecurityToken(
                new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)) { ["typ"] = "at+jwt" },
                new JwtPayload("https://identity.findyourfood.local", "findyourfood-local", claims,
                    DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5)));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                new JwtSecurityTokenHandler().WriteToken(jwt));
        }

        return client;
    }

    public OrganizationDbContext CreateDatabase() => new(_database!);

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        foreach (var (key, value) in _previous) Environment.SetEnvironmentVariable(key, value);
        if (_database is not null)
        {
            await using var db = CreateDatabase();
            await db.Database.EnsureDeletedAsync();
        }

        File.Delete(_keyPath);
        _key.Dispose();
    }

    private void Set(string name, string value)
    {
        _previous.Add(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }
}
