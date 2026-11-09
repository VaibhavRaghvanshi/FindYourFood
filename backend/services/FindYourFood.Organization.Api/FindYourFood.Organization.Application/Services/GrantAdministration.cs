using FindYourFood.Organization.Application.DTOs;
using FindYourFood.Organization.Application.Interfaces;
using FindYourFood.Organization.Domain;

namespace FindYourFood.Organization.Application.Services;

public sealed class GrantAdministration(IGrantRepository repository, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<IReadOnlyList<AccessGrant>> ListAsync(StaffActor actor, int page, CancellationToken cancellationToken) =>
        repository.ListAsync(actor, page, cancellationToken);

    public Task<GrantResult> GrantAsync(
        StaffActor actor, GrantRequest request, string traceId, CancellationToken cancellationToken)
    {
        if (request.SubjectId == Guid.Empty || !Enum.IsDefined(request.Level) ||
            !StaffAccess.IsKnownPermission(request.Permission) || !StaffAccess.IsScope(request.Scope) ||
            (request.Level == StaffLevel.Employee && request.CanDelegate))
        {
            return Task.FromResult(new GrantResult(GrantStatus.Invalid));
        }

        if (request.SubjectId == actor.Id || (actor.IsOwner && request.ParentGrantId is not null))
        {
            return Task.FromResult(new GrantResult(GrantStatus.Forbidden));
        }

        return unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!actor.IsOwner)
            {
                var chain = request.ParentGrantId is Guid parentId
                    ? await LoadActiveChainAsync(parentId, cancellationToken)
                    : [];
                if (chain.Count == 0 || chain[0].SubjectId != actor.Id ||
                    !StaffAccess.CanDelegate(chain[0], request) ||
                    chain.Any(ancestor => ancestor.SubjectId == request.SubjectId))
                {
                    return new GrantResult(GrantStatus.Forbidden);
                }
            }

            if (await repository.HasUnrevokedExactAsync(request, cancellationToken))
            {
                return new GrantResult(GrantStatus.Conflict);
            }

            var now = clock.GetUtcNow();
            var grant = new AccessGrant(Guid.NewGuid(), request.SubjectId, request.Level, request.Permission,
                request.Scope, request.CanDelegate, request.ParentGrantId, actor.Id, now);
            repository.Add(grant);
            repository.AddAudit(new GrantAudit(Guid.NewGuid(), grant.Id, actor.Id, "granted", traceId, now));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new GrantResult(GrantStatus.Accepted, grant);
        }, cancellationToken);
    }

    public Task<GrantResult> RevokeAsync(StaffActor actor, Guid id, string traceId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var target = await repository.FindAsync(id, cancellationToken);
            if (target is null)
            {
                return new GrantResult(actor.IsOwner ? GrantStatus.Missing : GrantStatus.Forbidden);
            }

            if (!actor.IsOwner)
            {
                var chain = target.ParentGrantId is Guid parentId
                    ? await LoadActiveChainAsync(parentId, cancellationToken)
                    : [];
                if (target.SubjectId == actor.Id || !chain.Any(ancestor => ancestor.SubjectId == actor.Id))
                {
                    return new GrantResult(GrantStatus.Forbidden);
                }
            }

            if (target.RevokedAt is null)
            {
                var now = clock.GetUtcNow();
                target.Revoke(now);
                repository.AddAudit(new GrantAudit(Guid.NewGuid(), target.Id, actor.Id, "revoked", traceId, now));
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new GrantResult(GrantStatus.Accepted, target);
        }, cancellationToken);

    public async Task<bool> CheckAsync(StaffActor actor, string permission, string scope, CancellationToken cancellationToken)
    {
        if (!StaffAccess.IsKnownPermission(permission) || !StaffAccess.IsScope(scope))
        {
            return false;
        }

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (actor.IsOwner)
            {
                return true;
            }

            foreach (var grant in await repository.FindCandidatesAsync(actor.Id, permission, scope, cancellationToken))
            {
                if ((await LoadActiveChainAsync(grant.Id, cancellationToken)).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<AccessGrant>> LoadActiveChainAsync(Guid id, CancellationToken cancellationToken)
    {
        var chain = new List<AccessGrant>();
        Guid? next = id;
        while (next is Guid current && chain.Count < (int)StaffLevel.Employee)
        {
            var grant = await repository.FindAsync(current, cancellationToken);
            if (grant is null || grant.RevokedAt is not null)
            {
                return [];
            }

            chain.Add(grant);
            next = grant.ParentGrantId;
        }

        return next is null ? chain : [];
    }
}
