using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using FindYourFood.Hosting;
using FindYourFood.Identity.Application.DTOs;
using FindYourFood.Identity.Domain;
using Microsoft.IdentityModel.Tokens;

namespace FindYourFood.Identity.Api.Authentication;

public sealed class DevelopmentTokenIssuer : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly RSA _rsa = RSA.Create();
    private readonly TokenSettings _settings;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _credentials;

    public DevelopmentTokenIssuer(TokenSettings settings, string privateKeyPath, TimeProvider clock)
    {
        _settings = settings;
        _clock = clock;
        _rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
        var key = new RsaSecurityKey(_rsa.ExportParameters(true));
        var publicKey = settings.LoadPublicKey();
        if (!key.Parameters.Modulus!.AsSpan().SequenceEqual(publicKey.Parameters.Modulus) ||
            !key.Parameters.Exponent!.AsSpan().SequenceEqual(publicKey.Parameters.Exponent))
        {
            throw new InvalidOperationException("Identity's signing key does not match its configured public key.");
        }

        _credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    public AccessTokenResponse Issue(Guid customerId) => Issue(customerId, accountType: null);

    public AccessTokenResponse IssuePrivileged(PrivilegedAccount account) =>
        Issue(account.Id, account.IsOwner ? "owner" : "staff");

    private AccessTokenResponse Issue(Guid subjectId, string? accountType)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var payload = new JwtPayload(_settings.Issuer, _settings.Audience, null, now, now.Add(Lifetime), now)
        {
            ["sub"] = subjectId.ToString("D"),
            ["jti"] = Guid.NewGuid().ToString("D")
        };
        if (accountType is not null)
        {
            payload["account_type"] = accountType;
            payload["amr"] = "mfa";
        }

        var jwt = new JwtSecurityToken(
            new JwtHeader(_credentials) { ["typ"] = "at+jwt" },
            payload);
        return new AccessTokenResponse(new JwtSecurityTokenHandler().WriteToken(jwt), (int)Lifetime.TotalSeconds);
    }

    public void Dispose() => _rsa.Dispose();
}
