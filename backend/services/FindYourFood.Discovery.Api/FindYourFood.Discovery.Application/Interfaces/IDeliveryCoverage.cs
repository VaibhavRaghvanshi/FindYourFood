namespace FindYourFood.Discovery.Application.Interfaces;

public sealed record GeoPoint(double Latitude, double Longitude);
public sealed record DeliveryCoverage(bool Serviceable, int? DeliveryFeeMinor, double? DistanceKm);

public interface IDeliveryCoverage
{
    Task<DeliveryCoverage[]> MatrixAsync(GeoPoint destination, GeoPoint[] origins, CancellationToken ct);
}
