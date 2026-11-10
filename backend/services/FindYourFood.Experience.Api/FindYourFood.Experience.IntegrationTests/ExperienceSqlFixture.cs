using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FindYourFood.Experience.IntegrationTests;

public sealed class ExperienceSqlFixture : IAsyncLifetime
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"fyf-experience-{Guid.NewGuid():N}.pem");
    private readonly Dictionary<string, string?> _previous = [];
    private DbContextOptions<ExperienceDbContext>? _database;
    private WebApplicationFactory<ExperienceHost>? _factory;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FINDYOURFOOD_TEST_SQL")
            ?? throw new InvalidOperationException("Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.");
        var isolated = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = $"FindYourFood_ExperienceTest_{Guid.NewGuid():N}"
        };
        _database = new DbContextOptionsBuilder<ExperienceDbContext>().UseSqlServer(isolated.ConnectionString).Options;
        await using var db = new ExperienceDbContext(_database);
        await db.Database.MigrateAsync();

        await File.WriteAllTextAsync(_keyPath, _key.ExportSubjectPublicKeyInfoPem());
        Set("DOTNET_ENVIRONMENT", "Development");
        Set("ASPNETCORE_ENVIRONMENT", "Development");
        Set("ConnectionStrings__Experience", isolated.ConnectionString);
        Set("Tokens__PublicKeyPath", _keyPath);
        _factory = new WebApplicationFactory<ExperienceHost>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped<IExperienceAccess, TestAccess>()));
    }

    public HttpClient Client(string? role = null)
    {
        var client = _factory!.CreateClient();
        if (role is not null)
        {
            var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString("D")), new("test_access", role) };
            if (role != "customer")
            {
                claims.Add(new Claim("account_type", "staff"));
                claims.Add(new Claim("amr", "mfa"));
            }

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

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        foreach (var (key, value) in _previous) Environment.SetEnvironmentVariable(key, value);
        if (_database is not null)
        {
            await using var db = new ExperienceDbContext(_database);
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

    private sealed class TestAccess : IExperienceAccess
    {
        public Task<AccessDecision> CheckAsync(string permission, string bearerToken, CancellationToken cancellationToken)
        {
            var role = new JwtSecurityTokenHandler().ReadJwtToken(bearerToken).Claims
                .Single(claim => claim.Type == "test_access").Value;
            return Task.FromResult(role switch
            {
                "editor" when permission == ExperiencePermissions.Edit => AccessDecision.Allowed,
                "publisher" when permission == ExperiencePermissions.Publish => AccessDecision.Allowed,
                "outage" => AccessDecision.Unavailable,
                _ => AccessDecision.Denied
            });
        }
    }
}
