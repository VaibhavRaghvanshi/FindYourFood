namespace FindYourFood.Discovery.Domain;

public sealed class CityIndex(string city)
{
    public string City { get; private set; } = city;
    public long Revision { get; private set; }
    public DateTimeOffset RefreshedAt { get; private set; }

    public void Refresh(DateTimeOffset now)
    {
        Revision++;
        RefreshedAt = now;
    }
}

public sealed class BranchProjection(string city, Guid branchId, string documentJson)
{
    public string City { get; private set; } = city;
    public Guid BranchId { get; private set; } = branchId;
    public string DocumentJson { get; private set; } = documentJson;

    public void Replace(string json) => DocumentJson = json;
}
