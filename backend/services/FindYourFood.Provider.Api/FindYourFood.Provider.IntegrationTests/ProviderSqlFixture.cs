using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using FindYourFood.Hosting;
using FindYourFood.Provider.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FindYourFood.Provider.IntegrationTests;

public sealed class ProviderSqlFixture : IAsyncLifetime
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"fyf-provider-{Guid.NewGuid():N}.pem");
    private readonly Dictionary<string, string?> _previous = [];
    private DbContextOptions<ProviderDbContext>? _database;
    private WebApplicationFactory<ProviderHost>? _factory;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FINDYOURFOOD_TEST_SQL")
            ?? throw new InvalidOperationException("Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.");
        var isolated = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = $"FindYourFood_ProviderTest_{Guid.NewGuid():N}"
        };
        _database = new DbContextOptionsBuilder<ProviderDbContext>().UseSqlServer(isolated.ConnectionString).Options;
        await using var db = new ProviderDbContext(_database);
        await db.Database.MigrateAsync();

        await File.WriteAllTextAsync(_keyPath, _key.ExportSubjectPublicKeyInfoPem());
        Set("DOTNET_ENVIRONMENT", "Development");
        Set("ASPNETCORE_ENVIRONMENT", "Development");
        Set("ConnectionStrings__Provider", isolated.ConnectionString);
        Set("Tokens__PublicKeyPath", _keyPath);
        _factory = new WebApplicationFactory<ProviderHost>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddHttpClient<OrganizationPermissionClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new PermissionHandler())));
    }

    public HttpClient Client(string? role = null, Guid? subject = null)
    {
        var client = _factory!.CreateClient();
        if (role is not null)
        {
            var claims = new List<Claim> { new("sub", (subject ?? Guid.NewGuid()).ToString("D")), new("test_access", role) };
            if (role != "applicant")
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
            await using var db = new ProviderDbContext(_database);
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

    private sealed class PermissionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var role = new JwtSecurityTokenHandler().ReadJwtToken(request.Headers.Authorization!.Parameter).Claims
                .Single(claim => claim.Type == "test_access").Value;
            if (role == "outage")
            {
                throw new HttpRequestException("Synthetic outage.");
            }

            var query = Uri.UnescapeDataString(request.RequestUri!.Query);
            var allowed = query.StartsWith("?permission=provider.review&scope=/providers", StringComparison.Ordinal) &&
                (role == "reviewer" || query == $"?permission=provider.review&scope=/providers/{role}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { allowed })
            });
        }
    }
}
