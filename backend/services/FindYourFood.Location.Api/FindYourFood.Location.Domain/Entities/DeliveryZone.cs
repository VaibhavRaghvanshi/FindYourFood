namespace FindYourFood.Location.Domain;

public sealed record ZoneSettings(string Name, string City, Coordinates Center, double RadiusKm,
    int DeliveryFeeMinor, bool Enabled)
{
    public const double MaximumRadiusKm = 80;

    public bool IsValid() => Text(Name) && Text(City) && Center?.IsValid == true &&
        RadiusKm is >= 0.1 and <= MaximumRadiusKm && DeliveryFeeMinor is >= 0 and <= 100_000;

    private static bool Text(string? text) => !string.IsNullOrWhiteSpace(text) &&
        text.Length <= 100 && text == text.Trim() && !text.Any(char.IsControl);
}

public sealed class DeliveryZone(Guid id)
{
    public Guid Id { get; private set; } = id;
    public long Revision { get; private set; }
    public string Name { get; private set; } = "";
    public string City { get; private set; } = "";
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double RadiusKm { get; private set; }
    public int DeliveryFeeMinor { get; private set; }
    public bool Enabled { get; private set; }

    public ZoneSettings Settings() =>
        new(Name, City, new(Latitude, Longitude), RadiusKm, DeliveryFeeMinor, Enabled);

    public void Save(ZoneSettings settings)
    {
        Name = settings.Name;
        City = settings.City;
        Latitude = settings.Center.Latitude;
        Longitude = settings.Center.Longitude;
        RadiusKm = settings.RadiusKm;
        DeliveryFeeMinor = settings.DeliveryFeeMinor;
        Enabled = settings.Enabled;
        Revision++;
    }
}
