using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Domain;

namespace FindYourFood.Experience.Application.Interfaces;

public interface IExperienceRepository
{
    Task<ExperienceWorkspace> WorkspaceAsync(CancellationToken cancellationToken);
    Task<ExperiencePublication?> LatestAsync(CancellationToken cancellationToken);
    Task<ExperiencePublication?> FindAsync(int version, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicationSummary>> HistoryAsync(CancellationToken cancellationToken);
    void Add(ExperiencePublication publication);
}
