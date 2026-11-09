using System.Data;
using FindYourFood.Organization.Application;
using FindYourFood.Organization.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Organization.Infrastructure;

public sealed class SqlGrantStore(OrganizationDbContext database) : IGrantStore
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

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error) when (error is SqlException { Number: 1205 or 1222 } ||
            error.InnerException is SqlException { Number: 1205 or 1222 })
        {
            throw new GrantContentionException(error);
        }
    }

    public async Task SaveAsync(AccessGrant grant, GrantAudit audit, bool isNew, CancellationToken cancellationToken)
    {
        if (isNew)
        {
            database.Grants.Add(grant);
        }

        database.Audit.Add(audit);
        await database.SaveChangesAsync(cancellationToken);
    }
}
