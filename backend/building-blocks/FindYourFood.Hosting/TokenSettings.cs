using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FindYourFood.Hosting;

public sealed record TokenSettings(string Issuer, string Audience, string PublicKeyPath)
{
    public RsaSecurityKey LoadPublicKey()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(PublicKeyPath));
        if (rsa.KeySize < 2048)
        {
            throw new InvalidOperationException("Access tokens require an RSA key of at least 2048 bits.");
        }

        return new RsaSecurityKey(rsa.ExportParameters(false));
    }

    public static TokenSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Tokens").Get<TokenSettings>();
        if (settings is null || string.IsNullOrWhiteSpace(settings.PublicKeyPath) ||
            string.IsNullOrWhiteSpace(settings.Audience) ||
            !Uri.TryCreate(settings.Issuer, UriKind.Absolute, out var issuer) || issuer.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Configure Tokens:Issuer, Tokens:Audience, and Tokens:PublicKeyPath.");
        }

        return settings;
    }

    public void ConfigureBearer(IServiceCollection services)
    {
        var key = LoadPublicKey();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidTypes = ["at+jwt"],
                ClockSkew = TimeSpan.FromSeconds(15)
            };
        });
        services.AddAuthorization();
    }
}
