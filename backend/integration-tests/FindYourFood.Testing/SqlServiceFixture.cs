using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FindYourFood.Testing;

public abstract class SqlServiceFixture<THost, TDatabase>(string service, Func<DbContextOptions<TDatabase>, TDatabase> createDatabase)
    : IAsyncLifetime where THost : class where TDatabase : DbContext
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"fyf-{Guid.NewGuid():N}.pem");
    private readonly Dictionary<string, string?> _previous = [];
    private DbContextOptions<TDatabase>? _options;
    private WebApplicationFactory<THost>? _factory;

    public async Task InitializeAsync()
    {
        var sql = Environment.GetEnvironmentVariable("FINDYOURFOOD_TEST_SQL")
            ?? throw new InvalidOperationException("Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.");
        var connection = new SqlConnectionStringBuilder(sql) { InitialCatalog = $"FindYourFood_{service}Test_{Guid.NewGuid():N}" };
        _options = new DbContextOptionsBuilder<TDatabase>().UseSqlServer(connection.ConnectionString).Options;
        await using (var database = createDatabase(_options))
        {
            await database.Database.MigrateAsync();
        }

        await File.WriteAllTextAsync(_keyPath, _key.ExportSubjectPublicKeyInfoPem());
        Set("DOTNET_ENVIRONMENT", "Development");
        Set("ASPNETCORE_ENVIRONMENT", "Development");
        Set($"ConnectionStrings__{service}", connection.ConnectionString);
        Set("Tokens__PublicKeyPath", _keyPath);
        _factory = new WebApplicationFactory<THost>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(Configure));
    }

    protected virtual void Configure(IServiceCollection services) { }

    public IServiceScope CreateScope() => _factory!.Services.CreateScope();

    public HttpClient Client(Guid? subject = null, params Claim[] additionalClaims)
    {
        var client = _factory!.CreateClient();
        if (subject is Guid id)
        {
            var claims = additionalClaims.Prepend(new Claim("sub", id.ToString("D")));
            var key = new RsaSecurityKey(_key.ExportParameters(true));
            var jwt = new JwtSecurityToken(
                new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)) { ["typ"] = "at+jwt" },
                new JwtPayload("https://identity.findyourfood.local", "findyourfood-local", claims,
                    DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10)));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                new JwtSecurityTokenHandler().WriteToken(jwt));
        }

        return client;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        foreach (var (key, value) in _previous) Environment.SetEnvironmentVariable(key, value);
        if (_options is not null)
        {
            await using var database = createDatabase(_options);
            await database.Database.EnsureDeletedAsync();
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
