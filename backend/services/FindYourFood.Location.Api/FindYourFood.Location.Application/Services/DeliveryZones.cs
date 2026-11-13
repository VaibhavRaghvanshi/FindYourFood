using FindYourFood.Location.Application.DTOs;
using FindYourFood.Location.Application.Interfaces;
using FindYourFood.Location.Domain;

namespace FindYourFood.Location.Application.Services;

public sealed class DeliveryZones(IZoneRepository zones, IUnitOfWork unitOfWork, IRouteEstimator routes)
{
    public async Task<Serviceability> CheckAsync(Coordinates point, CancellationToken ct)
    {
        var candidates = await zones.CandidatesAsync(point.Latitude, ct);
        var match = candidates.Select(zone => (Zone: zone, Distance: point.DistanceTo(new(zone.Latitude, zone.Longitude))))
            .Where(pair => pair.Distance <= pair.Zone.RadiusKm)
            .OrderBy(pair => pair.Distance).ThenBy(pair => pair.Zone.Id).FirstOrDefault();
        return match.Zone is null ? new(false) : new(true, match.Zone.Id, match.Zone.Name, match.Zone.City,
            match.Zone.DeliveryFeeMinor, Math.Round(match.Distance, 2), "straight-line-to-zone-center");
    }

    public async Task<ZoneSnapshot[]> PageAsync(int page, CancellationToken ct) =>
        (await zones.PageAsync(page, ct)).Select(Snapshot).ToArray();

    public async Task<Serviceability> QuoteAsync(DeliveryQuoteRequest input, CancellationToken ct)
    {
        var candidates = await zones.CandidatesAsync(input.Destination.Latitude, ct);
        return await QuoteAsync(input, candidates, ct);
    }

    public async Task<Serviceability[]> MatrixAsync(CoverageMatrixRequest input, CancellationToken ct)
    {
        var candidates = await zones.CandidatesAsync(input.Destination.Latitude, ct);
        var results = new List<Serviceability>(input.Origins.Count);
        foreach (var origin in input.Origins)
        {
            results.Add(await QuoteAsync(new(origin, input.Destination), candidates, ct));
        }
        return results.ToArray();
    }

    private async Task<Serviceability> QuoteAsync(DeliveryQuoteRequest input, IReadOnlyList<DeliveryZone> candidates, CancellationToken ct)
    {
        var match = candidates.Where(zone =>
                input.Origin.DistanceTo(new(zone.Latitude, zone.Longitude)) <= zone.RadiusKm &&
                input.Destination.DistanceTo(new(zone.Latitude, zone.Longitude)) <= zone.RadiusKm)
            .OrderBy(zone => zone.DeliveryFeeMinor).ThenBy(zone => zone.Id).FirstOrDefault();
        if (match is null) return new(false);
        var estimate = await routes.EstimateAsync(input.Origin, input.Destination, ct);
        return new(true, match.Id, match.Name, match.City,
            match.DeliveryFeeMinor, estimate.DistanceKm, estimate.Method);
    }

    public async Task<ZoneSaveResult> SaveAsync(Guid id, ZoneUpdate input, CancellationToken ct)
    {
        if (id == Guid.Empty || input.Revision < 0 || input.Settings is null || !input.Settings.IsValid())
        {
            return new(ZoneSaveStatus.Invalid);
        }

        var zone = await zones.FindAsync(id, ct);
        if ((zone?.Revision ?? 0) != input.Revision) return new(ZoneSaveStatus.Conflict);
        if (zone is null)
        {
            zone = new DeliveryZone(id);
            zones.Add(zone);
        }

        zone.Save(input.Settings);
        return await unitOfWork.TrySaveAsync(ct)
            ? new(ZoneSaveStatus.Saved, Snapshot(zone)) : new(ZoneSaveStatus.Conflict);
    }

    private static ZoneSnapshot Snapshot(DeliveryZone zone) => new(zone.Id, zone.Revision, zone.Settings());
}
