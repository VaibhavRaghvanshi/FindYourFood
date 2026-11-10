namespace FindYourFood.Catalog.Domain;

public sealed class BranchMenu(Guid branchId)
{
    public Guid BranchId { get; private set; } = branchId;
    public string? DraftJson { get; private set; }
    public int Revision { get; private set; }
    public int PublishedVersion { get; private set; }
    public bool HasUnpublishedChanges { get; private set; }

    public void Save(string content)
    {
        DraftJson = content;
        HasUnpublishedChanges = true;
        Revision++;
    }

    public MenuRelease Publish(DateTimeOffset now)
    {
        if (!HasUnpublishedChanges || DraftJson is null)
        {
            throw new InvalidOperationException("Save a draft before publishing.");
        }

        Revision++;
        PublishedVersion++;
        HasUnpublishedChanges = false;
        return new MenuRelease(BranchId, PublishedVersion, DraftJson, now);
    }
}

public sealed record MenuRelease(Guid BranchId, int Version, string ContentJson, DateTimeOffset PublishedAt);
