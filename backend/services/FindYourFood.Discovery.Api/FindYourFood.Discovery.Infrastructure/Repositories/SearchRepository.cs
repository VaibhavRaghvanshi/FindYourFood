using FindYourFood.Discovery.Application.Interfaces;
using FindYourFood.Discovery.Domain;
using FindYourFood.Discovery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Discovery.Infrastructure.Repositories;

internal sealed class SearchRepository(DiscoveryDbContext database) : ISearchRepository
{
    public Task<CityIndex?> IndexAsync(string city, CancellationToken ct) =>
        database.Indexes.SingleOrDefaultAsync(index => index.City == city, ct);

    public async Task<IReadOnlyList<BranchProjection>> CityAsync(string city, CancellationToken ct) =>
        await database.Projections.Where(row => row.City == city).ToListAsync(ct);

    public void Add(CityIndex index) => database.Indexes.Add(index);
    public void Add(BranchProjection projection) => database.Projections.Add(projection);
    public void Remove(BranchProjection projection) => database.Projections.Remove(projection);
}
