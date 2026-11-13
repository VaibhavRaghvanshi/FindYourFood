using FindYourFood.Location.Application.Interfaces;
using FindYourFood.Location.Domain;

namespace FindYourFood.Location.Infrastructure.Routing;

internal sealed class StraightLineEstimator : IRouteEstimator
{
    public Task<RouteEstimate> EstimateAsync(Coordinates origin, Coordinates destination, CancellationToken ct) =>
        Task.FromResult(new RouteEstimate(Math.Round(origin.DistanceTo(destination), 2),
            "straight-line-origin-to-destination"));
}
