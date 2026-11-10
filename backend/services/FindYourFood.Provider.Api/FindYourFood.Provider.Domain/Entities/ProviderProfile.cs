namespace FindYourFood.Provider.Domain;

public sealed class ProviderProfile
{
    private ProviderProfile() { }

    public ProviderProfile(Guid ownerId, ProviderDetails details, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Update(details, now);
    }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProviderStatus Status { get; private set; }
    public int Revision { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public bool CanEditApplication => Status is ProviderStatus.Draft or ProviderStatus.Rejected;

    public void Update(ProviderDetails details, DateTimeOffset now)
    {
        Name = details.Name.Trim();
        Kind = details.Kind;
        ContactEmail = details.ContactEmail.Trim();
        Description = details.Description.Trim();
        Touch(now);
    }

    public void Submit(DateTimeOffset now)
    {
        Status = ProviderStatus.Submitted;
        ReviewNote = null;
        ReviewedBy = null;
        ReviewedAt = null;
        Touch(now);
    }

    public void Review(Guid reviewer, bool approved, string? note, DateTimeOffset now)
    {
        Status = approved ? ProviderStatus.Approved : ProviderStatus.Rejected;
        ReviewNote = note?.Trim();
        ReviewedBy = reviewer;
        ReviewedAt = now;
        Touch(now);
    }

    public void Touch(DateTimeOffset now)
    {
        Revision++;
        UpdatedAt = now;
    }
}
