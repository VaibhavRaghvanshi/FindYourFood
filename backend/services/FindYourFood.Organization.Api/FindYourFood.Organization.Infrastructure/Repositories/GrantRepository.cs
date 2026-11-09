using FindYourFood.Organization.Application;
using FindYourFood.Organization.Application.Interfaces;
using FindYourFood.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Organization.Infrastructure.Repositories;

internal sealed class GrantRepository(OrganizationDbContext database) : IGrantRepository
{
    public Task<AccessGrant?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        database.Grants.SingleOrDefaultAsync(grant => grant.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AccessGrant>> ListAsync(StaffActor actor, int page, CancellationToken cancellationToken)
    {
        var visible = database.Grants.AsNoTracking();
        if (!actor.IsOwner)
        {
            visible = visible.Where(grant => grant.SubjectId == actor.Id || grant.GrantedBy == actor.Id);
        }

        return await visible.OrderByDescending(grant => grant.GrantedAt).ThenBy(grant => grant.Id)
            .Skip(page * 50).Take(50).ToListAsync(cancellationToken);
    }

    public Task<bool> HasUnrevokedExactAsync(GrantRequest request, CancellationToken cancellationToken) =>
        database.Grants.AnyAsync(grant => grant.SubjectId == request.SubjectId && grant.Permission == request.Permission &&
            grant.Scope == request.Scope && grant.RevokedAt == null, cancellationToken);

    public async Task<IReadOnlyList<AccessGrant>> FindCandidatesAsync(
        Guid subject, string permission, string scope, CancellationToken cancellationToken)
    {
        var scopes = new List<string> { scope };
        while (scope.Length > 1)
        {
            scope = scope[..Math.Max(1, scope.LastIndexOf('/'))];
            scopes.Add(scope);
        }

        return await database.Grants.AsNoTracking()
            .Where(grant => grant.SubjectId == subject && grant.Permission == permission &&
                grant.RevokedAt == null && scopes.Contains(grant.Scope))
            .ToListAsync(cancellationToken);
    }

    public void Add(AccessGrant grant) => database.Grants.Add(grant);

    public void AddAudit(GrantAudit audit) => database.Audit.Add(audit);
}
