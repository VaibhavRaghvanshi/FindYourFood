using System.Security.Cryptography;
using FindYourFood.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FindYourFood.Identity.IntegrationTests;

public sealed class IdentitySqlFixture : IAsyncLifetime
{
    private readonly string _keys = Path.Combine(Path.GetTempPath(), "findyourfood-tests-" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string?> _previousSettings = new();
    private DbContextOptions<IdentityDbContext>? _database;
    private WebApplicationFactory<Program>? _factory;

    public HttpClient CreateClient() => _factory!.CreateClient();

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FINDYOURFOOD_TEST_SQL")
            ?? throw new InvalidOperationException("Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.");
        var testConnection = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = "FindYourFood_IdentityTest_" + Guid.NewGuid().ToString("N")
        };
        _database = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(testConnection.ConnectionString).Options;
        await using var db = new IdentityDbContext(_database);
        await db.Database.MigrateAsync();

        Directory.CreateDirectory(_keys);
        using var rsa = RSA.Create(2048);
        var publicKey = Path.Combine(_keys, "public.pem");
        var privateKey = Path.Combine(_keys, "private.pem");
        await File.WriteAllTextAsync(publicKey, rsa.ExportSubjectPublicKeyInfoPem());
        await File.WriteAllTextAsync(privateKey, rsa.ExportPkcs8PrivateKeyPem());

        SetSetting("ASPNETCORE_ENVIRONMENT", "Development");
        SetSetting("DOTNET_ENVIRONMENT", "Development");
        SetSetting("ConnectionStrings__Identity", testConnection.ConnectionString);
        SetSetting("Tokens__PublicKeyPath", publicKey);
        SetSetting("Tokens__PrivateKeyPath", privateKey);
        _factory = new WebApplicationFactory<Program>();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        foreach (var (key, value) in _previousSettings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        if (_database is not null)
        {
            await using var db = new IdentityDbContext(_database);
            await db.Database.EnsureDeletedAsync();
        }

        if (Directory.Exists(_keys))
        {
            File.Delete(Path.Combine(_keys, "public.pem"));
            File.Delete(Path.Combine(_keys, "private.pem"));
            Directory.Delete(_keys);
        }
    }

    private void SetSetting(string key, string value)
    {
        _previousSettings.Add(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }
}
