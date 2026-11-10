using System.Text.Json;
using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Domain;

namespace FindYourFood.Experience.Application.Services;

public sealed class ExperiencePublishing(
    IExperienceRepository repository, IUnitOfWork unitOfWork, IPublishedExperienceCache cache, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<PublishedExperience?> PublishedAsync(CancellationToken cancellationToken) =>
        cache.GetAsync(async ct =>
        {
            var publication = await repository.LatestAsync(ct);
            return publication is null ? null : new PublishedExperience(publication.Version, Read(publication.ConfigurationJson));
        }, cancellationToken);

    public async Task<WorkspaceView> WorkspaceAsync(CancellationToken cancellationToken) =>
        await ViewAsync(await repository.WorkspaceAsync(cancellationToken), cancellationToken);

    public Task<WorkflowResult> SaveDraftAsync(SaveDraftRequest request, CancellationToken cancellationToken)
    {
        if (!ExperienceRules.IsValid(request.Configuration))
        {
            return Task.FromResult(new WorkflowResult(WorkflowStatus.Invalid));
        }

        return ChangeAsync(request.ExpectedRevision, (state, _) =>
        {
            state.SaveDraft(JsonSerializer.Serialize(request.Configuration, Json));
            return Task.FromResult(WorkflowStatus.Accepted);
        }, invalidate: false, cancellationToken);
    }

    public Task<WorkflowResult> ApproveAsync(int revision, Guid actor, CancellationToken cancellationToken) =>
        ChangeAsync(revision, (state, _) =>
        {
            if (state.DraftJson is null || state.ApprovedBy is not null)
            {
                return Task.FromResult(WorkflowStatus.Conflict);
            }

            state.Approve(actor);
            return Task.FromResult(WorkflowStatus.Accepted);
        }, invalidate: false, cancellationToken);

    public Task<WorkflowResult> PublishAsync(int revision, Guid actor, CancellationToken cancellationToken) =>
        ChangeAsync(revision, (state, _) =>
        {
            if (state.ApprovedBy is null || state.DraftPublished)
            {
                return Task.FromResult(WorkflowStatus.Conflict);
            }

            repository.Add(state.Publish(actor, clock.GetUtcNow()));
            return Task.FromResult(WorkflowStatus.Accepted);
        }, invalidate: true, cancellationToken);

    public Task<WorkflowResult> RollbackAsync(RollbackRequest request, Guid actor, CancellationToken cancellationToken) =>
        ChangeAsync(request.ExpectedRevision, async (state, ct) =>
        {
            if (request.Version == state.PublishedVersion)
            {
                return WorkflowStatus.Conflict;
            }

            var previous = await repository.FindAsync(request.Version, ct);
            if (previous is null)
            {
                return WorkflowStatus.Missing;
            }

            repository.Add(state.Restore(previous, actor, clock.GetUtcNow()));
            return WorkflowStatus.Accepted;
        }, invalidate: true, cancellationToken);

    private async Task<WorkflowResult> ChangeAsync(
        int revision, Func<ExperienceWorkspace, CancellationToken, Task<WorkflowStatus>> operation,
        bool invalidate, CancellationToken cancellationToken)
    {
        WorkflowResult result;
        try
        {
            result = await unitOfWork.ExecuteAsync(async () =>
            {
                var state = await repository.WorkspaceAsync(cancellationToken);
                if (state.Revision != revision)
                {
                    return new WorkflowResult(WorkflowStatus.Conflict);
                }

                var status = await operation(state, cancellationToken);
                if (status != WorkflowStatus.Accepted)
                {
                    return new WorkflowResult(status);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new WorkflowResult(status, await ViewAsync(state, cancellationToken));
            }, cancellationToken);
        }
        catch (ExperienceContentionException)
        {
            return new WorkflowResult(WorkflowStatus.Conflict);
        }

        if (invalidate && result.Status == WorkflowStatus.Accepted)
        {
            cache.Invalidate();
        }

        return result;
    }

    private async Task<WorkspaceView> ViewAsync(ExperienceWorkspace state, CancellationToken cancellationToken)
    {
        var status = state.DraftJson is null ? "empty" : state.DraftPublished ? "published" :
            state.ApprovedBy is null ? "draft" : "approved";
        return new WorkspaceView(state.Revision, state.DraftJson is null ? null : Read(state.DraftJson), status,
            state.PublishedVersion == 0 ? null : state.PublishedVersion, await repository.HistoryAsync(cancellationToken));
    }

    private static ExperienceDocument Read(string json) =>
        JsonSerializer.Deserialize<ExperienceDocument>(json, Json)
        ?? throw new InvalidOperationException("Stored experience configuration is missing.");
}
