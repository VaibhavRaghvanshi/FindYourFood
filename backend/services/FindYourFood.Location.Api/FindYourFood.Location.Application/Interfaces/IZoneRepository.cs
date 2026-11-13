using FindYourFood.Location.Domain;

namespace FindYourFood.Location.Application.Interfaces;

public interface IZoneRepository
{
    Task<IReadOnlyList<DeliveryZone>> CandidatesAsync(double latitude, CancellationToken ct);
    Task<IReadOnlyList<DeliveryZone>> PageAsync(int page, CancellationToken ct);
    Task<DeliveryZone?> FindAsync(Guid id, CancellationToken ct);
    void Add(DeliveryZone zone);
}
