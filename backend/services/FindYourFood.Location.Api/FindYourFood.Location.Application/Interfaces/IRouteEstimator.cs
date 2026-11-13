using FindYourFood.Location.Domain;

namespace FindYourFood.Location.Application.Interfaces;

public sealed record RouteEstimate(double DistanceKm, string Method);

public interface IRouteEstimator
{
    Task<RouteEstimate> EstimateAsync(Coordinates origin, Coordinates destination, CancellationToken ct);
}
