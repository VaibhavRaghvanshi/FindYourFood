using FindYourFood.Location.Application.Interfaces;
using FindYourFood.Location.Domain;
using FindYourFood.Location.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Location.Infrastructure.Repositories;

internal sealed class ZoneRepository(LocationDbContext database) : IZoneRepository
{
    public async Task<IReadOnlyList<DeliveryZone>> CandidatesAsync(double latitude, CancellationToken ct)
    {
        const double margin = ZoneSettings.MaximumRadiusKm / 110;
        return await database.Zones.AsNoTracking()
            .Where(zone => zone.Enabled && zone.Latitude >= latitude - margin && zone.Latitude <= latitude + margin)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DeliveryZone>> PageAsync(int page, CancellationToken ct) =>
        await database.Zones.AsNoTracking().OrderBy(zone => zone.Name).ThenBy(zone => zone.Id)
            .Skip((page - 1) * 25).Take(25).ToListAsync(ct);

    public Task<DeliveryZone?> FindAsync(Guid id, CancellationToken ct) =>
        database.Zones.SingleOrDefaultAsync(zone => zone.Id == id, ct);

    public void Add(DeliveryZone zone) => database.Zones.Add(zone);
}
