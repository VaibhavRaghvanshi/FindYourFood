using FindYourFood.Organization.Domain;

namespace FindYourFood.Organization.Application.Interfaces;

public interface IGrantRepository
{
    Task<AccessGrant?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccessGrant>> ListAsync(StaffActor actor, int page, CancellationToken cancellationToken);
    Task<bool> HasUnrevokedExactAsync(GrantRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccessGrant>> FindCandidatesAsync(Guid subject, string permission, string scope, CancellationToken cancellationToken);
    void Add(AccessGrant grant);
    void AddAudit(GrantAudit audit);
}
