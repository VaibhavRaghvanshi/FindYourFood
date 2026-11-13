namespace FindYourFood.Location.Domain;

public sealed record Coordinates(double Latitude, double Longitude)
{
    public bool IsValid => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;

    public double DistanceTo(Coordinates other)
    {
        const double radiusKm = 6371.0088;
        const double radians = Math.PI / 180;
        var latitude = Math.Sin((other.Latitude - Latitude) * radians / 2);
        var longitude = Math.Sin((other.Longitude - Longitude) * radians / 2);
        var haversine = latitude * latitude +
            Math.Cos(Latitude * radians) * Math.Cos(other.Latitude * radians) * longitude * longitude;
        return 2 * radiusKm * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }
}
