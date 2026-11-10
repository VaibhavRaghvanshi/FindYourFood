namespace FindYourFood.Experience.Domain;

public sealed class ExperienceWorkspace
{
    public const int CustomerHomeId = 1;
    public int Id { get; private set; } = CustomerHomeId;
    public int Revision { get; private set; }
    public string? DraftJson { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public bool DraftPublished { get; private set; }
    public int PublishedVersion { get; private set; }

    public void SaveDraft(string json)
    {
        DraftJson = json;
        ApprovedBy = null;
        DraftPublished = false;
        Revision++;
    }

    public void Approve(Guid actor)
    {
        ApprovedBy = actor;
        Revision++;
    }

    public ExperiencePublication Publish(Guid actor, DateTimeOffset publishedAt)
    {
        if (DraftJson is null || ApprovedBy is null || DraftPublished)
        {
            throw new InvalidOperationException("Only an approved draft can be published.");
        }

        DraftPublished = true;
        return Release(DraftJson, actor, publishedAt, ApprovedBy, restoredFrom: null);
    }

    public ExperiencePublication Restore(ExperiencePublication original, Guid actor, DateTimeOffset publishedAt) =>
        Release(original.ConfigurationJson, actor, publishedAt, approvedBy: null, restoredFrom: original.Version);

    private ExperiencePublication Release(
        string configuration, Guid actor, DateTimeOffset publishedAt, Guid? approvedBy, int? restoredFrom)
    {
        Revision++;
        PublishedVersion++;
        return new ExperiencePublication(PublishedVersion, configuration, actor, publishedAt, approvedBy, restoredFrom);
    }
}
