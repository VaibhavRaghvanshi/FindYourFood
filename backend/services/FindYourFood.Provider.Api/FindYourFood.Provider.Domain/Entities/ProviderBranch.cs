namespace FindYourFood.Provider.Domain;

public sealed class ProviderBranch
{
    private ProviderBranch() { }

    public ProviderBranch(Guid providerId, BranchDetails details, string hoursJson)
    {
        Id = Guid.NewGuid();
        ProviderId = providerId;
        Update(details, hoursJson);
    }

    public Guid Id { get; private set; }
    public Guid ProviderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = string.Empty;
    public string HoursJson { get; private set; } = string.Empty;
    public DateTimeOffset? ClosedUntil { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public void Update(BranchDetails details, string hoursJson)
    {
        Name = details.Name.Trim();
        Address = details.Address.Trim();
        City = details.City.Trim();
        TimeZone = details.TimeZone;
        HoursJson = hoursJson;
        ClosedUntil = details.ClosedUntil;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
    }
}
