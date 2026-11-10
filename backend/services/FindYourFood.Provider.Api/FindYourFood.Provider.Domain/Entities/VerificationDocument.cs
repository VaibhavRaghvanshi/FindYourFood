namespace FindYourFood.Provider.Domain;

public sealed class VerificationDocument(Guid providerId, string kind, string mediaType, byte[] content, DateTimeOffset uploadedAt)
{
    public Guid ProviderId { get; private set; } = providerId;
    public string Kind { get; private set; } = kind;
    public string MediaType { get; private set; } = mediaType;
    public byte[] Content { get; private set; } = content;
    public int Size { get; private set; } = content.Length;
    public DateTimeOffset UploadedAt { get; private set; } = uploadedAt;

    public void Replace(string type, byte[] bytes, DateTimeOffset now)
    {
        MediaType = type;
        Content = bytes;
        Size = bytes.Length;
        UploadedAt = now;
    }

    private VerificationDocument() : this(Guid.Empty, string.Empty, string.Empty, [], default) { }
}
