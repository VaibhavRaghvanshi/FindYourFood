using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FindYourFood.Payment.Api.Workers;

public sealed class GatewaySignature(IConfiguration configuration, TimeProvider clock)
{
    public bool Verify(string timestamp, string signature, ReadOnlySpan<byte> body)
    {
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            signature.Length != 64 || !signature.All(Uri.IsHexDigit)) return false;
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        if (seconds < now - 300 || seconds > now + 300) return false;
        var path = configuration["Payment:WebhookKeyPath"];
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        var key = File.ReadAllBytes(path);
        if (key.Length < 32) return false;
        var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
        var payload = new byte[prefix.Length + body.Length];
        prefix.CopyTo(payload, 0);
        body.CopyTo(payload.AsSpan(prefix.Length));
        return CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, payload), Convert.FromHexString(signature));
    }
}
