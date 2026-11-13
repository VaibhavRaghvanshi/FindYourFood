using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Domain;

namespace FindYourFood.Provider.Application.Interfaces;

public interface IProviderRepository
{
    Task<IReadOnlyList<PublicBranchView>> PublicAsync(string? city, Guid? branch, int page, CancellationToken cancellationToken);
    Task<ProviderProfile?> FindOwnedAsync(Guid owner, CancellationToken cancellationToken);
    Task<ProviderProfile?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderSummary>> PendingAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentView>> DocumentsAsync(Guid provider, CancellationToken cancellationToken);
    Task<VerificationDocument?> DocumentAsync(Guid provider, string kind, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderBranch>> BranchesAsync(Guid provider, CancellationToken cancellationToken);
    Task<ProviderBranch?> BranchAsync(Guid provider, Guid branch, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderReview>> ReviewsAsync(Guid provider, CancellationToken cancellationToken);
    void Add(ProviderProfile provider);
    void Add(VerificationDocument document);
    void Add(ProviderBranch branch);
    void Add(ProviderReview review);
}
