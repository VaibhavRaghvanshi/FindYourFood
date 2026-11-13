using FindYourFood.Discovery.Domain;

namespace FindYourFood.Discovery.Application.Interfaces;

public interface ISearchRepository
{
    Task<CityIndex?> IndexAsync(string city, CancellationToken ct);
    Task<IReadOnlyList<BranchProjection>> CityAsync(string city, CancellationToken ct);
    void Add(CityIndex index);
    void Add(BranchProjection projection);
    void Remove(BranchProjection projection);
}
