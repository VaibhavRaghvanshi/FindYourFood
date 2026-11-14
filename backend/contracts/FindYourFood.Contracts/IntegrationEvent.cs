using System.Security.Cryptography;
using System.Text.Json;

namespace FindYourFood.Contracts;

public sealed record IntegrationEvent(Guid MessageId, string Source, string Type, Guid AggregateId,
    long AggregateVersion, DateTimeOffset OccurredAt, JsonElement Data)
{
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web) { MaxDepth = 32 };
    public bool IsValid() =>
        MessageId != Guid.Empty && AggregateId != Guid.Empty && AggregateVersion > 0 &&
        Source is { Length: > 0 and <= 32 } && Source.All(character => character is >= 'a' and <= 'z') &&
        Type is { Length: > 0 and <= 100 } && Type.StartsWith(Source + ".", StringComparison.Ordinal) &&
        Type.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-') &&
        Data.ValueKind == JsonValueKind.Object;

    public string ContentHash() => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this, Json)));
}

public interface IIntegrationEventHandler
{
    Task HandleAsync(IntegrationEvent message, CancellationToken ct);
}

public sealed class InvalidIntegrationEventException(string reason) : Exception(reason);
public sealed record InboxReceipt(Guid MessageId, string Source, string Type, string ContentHash, DateTimeOffset ProcessedAt);
