using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Domain;
using FindYourFood.Experience.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Experience.Infrastructure.Repositories;

internal sealed class ExperienceRepository(ExperienceDbContext database) : IExperienceRepository
{
    public Task<ExperienceWorkspace> WorkspaceAsync(CancellationToken cancellationToken) =>
        database.Workspaces.SingleAsync(state => state.Id == ExperienceWorkspace.CustomerHomeId, cancellationToken);

    public Task<ExperiencePublication?> LatestAsync(CancellationToken cancellationToken) =>
        database.Publications.AsNoTracking().OrderByDescending(release => release.Version).FirstOrDefaultAsync(cancellationToken);

    public Task<ExperiencePublication?> FindAsync(int version, CancellationToken cancellationToken) =>
        database.Publications.AsNoTracking().SingleOrDefaultAsync(release => release.Version == version, cancellationToken);

    public async Task<IReadOnlyList<PublicationSummary>> HistoryAsync(CancellationToken cancellationToken) =>
        await database.Publications.AsNoTracking().OrderByDescending(release => release.Version).Take(20)
            .Select(release => new PublicationSummary(release.Version, release.PublishedAt, release.PublishedBy,
                release.ApprovedBy, release.RestoredFromVersion)).ToListAsync(cancellationToken);

    public void Add(ExperiencePublication publication) => database.Publications.Add(publication);
}
