using System.Text.Json;
using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Application.Interfaces;
using FindYourFood.Provider.Domain;

namespace FindYourFood.Provider.Application.Services;

public sealed class ProviderOnboarding(IProviderRepository repository, IUnitOfWork unitOfWork, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ProviderView?> OwnedAsync(Guid owner, CancellationToken ct) =>
        await repository.FindOwnedAsync(owner, ct) is { } profile ? await ViewAsync(profile, ct) : null;

    public async Task<ProviderView?> FindAsync(Guid id, CancellationToken ct) =>
        await repository.FindAsync(id, ct) is { Status: not ProviderStatus.Draft } profile
            ? await ViewAsync(profile, ct)
            : null;

    public Task<IReadOnlyList<ProviderSummary>> PendingAsync(CancellationToken ct) => repository.PendingAsync(ct);

    public async Task<VerificationDocument?> OwnedDocumentAsync(Guid owner, string kind, CancellationToken ct) =>
        ProviderRules.ValidDocumentKind(kind) && await repository.FindOwnedAsync(owner, ct) is { } profile
            ? await repository.DocumentAsync(profile.Id, kind, ct)
            : null;

    public async Task<VerificationDocument?> DocumentAsync(Guid id, string kind, CancellationToken ct) =>
        ProviderRules.ValidDocumentKind(kind) &&
        await repository.FindAsync(id, ct) is { Status: ProviderStatus.Submitted or ProviderStatus.Approved }
            ? await repository.DocumentAsync(id, kind, ct)
            : null;

    public Task<ProviderResult> CreateAsync(Guid owner, ProviderDetails details, CancellationToken ct)
    {
        if (!ProviderRules.ValidDetails(details)) return Invalid();
        return WriteAsync(async () =>
        {
            if (await repository.FindOwnedAsync(owner, ct) is not null)
            {
                return new ProviderResult(ProviderResultStatus.Conflict);
            }

            var profile = new ProviderProfile(owner, details, clock.GetUtcNow());
            repository.Add(profile);
            await unitOfWork.SaveChangesAsync(ct);
            return new ProviderResult(ProviderResultStatus.Accepted, await ViewAsync(profile, ct));
        }, ct);
    }

    public Task<ProviderResult> UpdateAsync(Guid owner, UpdateProviderRequest request, CancellationToken ct)
    {
        if (!ProviderRules.ValidDetails(request.Details)) return Invalid();
        return ChangeAsync(() => repository.FindOwnedAsync(owner, ct), request.ExpectedRevision, profile =>
        {
            var canEditSettings = profile.Status == ProviderStatus.Approved &&
                request.Details.Name.Trim() == profile.Name && request.Details.Kind == profile.Kind;
            if (!profile.CanEditApplication && !canEditSettings) return Task.FromResult(ProviderResultStatus.Conflict);
            profile.Update(request.Details, clock.GetUtcNow());
            return Task.FromResult(ProviderResultStatus.Accepted);
        }, ct);
    }

    public Task<ProviderResult> UploadAsync(Guid owner, string kind, int revision, byte[] content, CancellationToken ct)
    {
        var mediaType = ProviderRules.DetectMediaType(content);
        if (!ProviderRules.ValidDocumentKind(kind) || mediaType is null) return Invalid();
        return ChangeAsync(() => repository.FindOwnedAsync(owner, ct), revision, async profile =>
        {
            if (!profile.CanEditApplication) return ProviderResultStatus.Conflict;
            var now = clock.GetUtcNow();
            var existing = await repository.DocumentAsync(profile.Id, kind, ct);
            if (existing is null)
            {
                repository.Add(new VerificationDocument(profile.Id, kind, mediaType, content, now));
            }
            else
            {
                existing.Replace(mediaType, content, now);
            }

            profile.Touch(now);
            return ProviderResultStatus.Accepted;
        }, ct);
    }

    public Task<ProviderResult> SubmitAsync(Guid owner, int revision, CancellationToken ct) =>
        ChangeAsync(() => repository.FindOwnedAsync(owner, ct), revision, async profile =>
        {
            if (!profile.CanEditApplication) return ProviderResultStatus.Conflict;
            var documents = await repository.DocumentsAsync(profile.Id, ct);
            if (!documents.Any(document => document.Kind == "license") ||
                !documents.Any(document => document.Kind == "address-proof"))
            {
                return ProviderResultStatus.Invalid;
            }

            profile.Submit(clock.GetUtcNow());
            return ProviderResultStatus.Accepted;
        }, ct);

    public Task<ProviderResult> ReviewAsync(Guid id, Guid reviewer, ReviewRequest request, CancellationToken ct)
    {
        if (!ProviderRules.ValidReview(request.Approved, request.Note)) return Invalid();
        return ChangeAsync(() => repository.FindAsync(id, ct), request.ExpectedRevision, profile =>
        {
            if (profile.Status != ProviderStatus.Submitted || profile.OwnerId == reviewer)
            {
                return Task.FromResult(ProviderResultStatus.Conflict);
            }

            var now = clock.GetUtcNow();
            profile.Review(reviewer, request.Approved, request.Note, now);
            repository.Add(new ProviderReview(Guid.NewGuid(), id, request.ExpectedRevision, reviewer,
                request.Approved, profile.ReviewNote, now));
            return Task.FromResult(ProviderResultStatus.Accepted);
        }, ct);
    }

    public Task<ProviderResult> SaveBranchAsync(Guid owner, Guid? branchId, UpdateBranchRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (!ProviderRules.ValidBranch(request.Branch, now)) return Invalid();
        return ChangeAsync(() => repository.FindOwnedAsync(owner, ct), request.ExpectedRevision, async profile =>
        {
            if (profile.Status != ProviderStatus.Approved) return ProviderResultStatus.Conflict;
            var hours = JsonSerializer.Serialize(request.Branch.Hours.OrderBy(day => day.Day), Json);
            if (branchId is Guid id)
            {
                var branch = await repository.BranchAsync(profile.Id, id, ct);
                if (branch is null) return ProviderResultStatus.Missing;
                branch.Update(request.Branch, hours);
            }
            else
            {
                if ((await repository.BranchesAsync(profile.Id, ct)).Count >= ProviderRules.MaxBranches)
                {
                    return ProviderResultStatus.Conflict;
                }

                repository.Add(new ProviderBranch(profile.Id, request.Branch, hours));
            }

            profile.Touch(now);
            return ProviderResultStatus.Accepted;
        }, ct);
    }

    private Task<ProviderResult> ChangeAsync(
        Func<Task<ProviderProfile?>> find, int revision, Func<ProviderProfile, Task<ProviderResultStatus>> change,
        CancellationToken ct) =>
        WriteAsync(async () =>
        {
            var profile = await find();
            if (profile is null) return new ProviderResult(ProviderResultStatus.Missing);
            if (profile.Revision != revision) return new ProviderResult(ProviderResultStatus.Conflict);

            var status = await change(profile);
            if (status != ProviderResultStatus.Accepted) return new ProviderResult(status);
            await unitOfWork.SaveChangesAsync(ct);
            return new ProviderResult(status, await ViewAsync(profile, ct));
        }, ct);

    private async Task<ProviderResult> WriteAsync(Func<Task<ProviderResult>> action, CancellationToken ct)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(action, ct);
        }
        catch (ProviderContentionException)
        {
            return new ProviderResult(ProviderResultStatus.Conflict);
        }
    }

    private async Task<ProviderView> ViewAsync(ProviderProfile profile, CancellationToken ct)
    {
        var documents = await repository.DocumentsAsync(profile.Id, ct);
        var branches = await repository.BranchesAsync(profile.Id, ct);
        var reviews = await repository.ReviewsAsync(profile.Id, ct);
        return new ProviderView(profile.Id, profile.Revision, profile.Status.ToString().ToLowerInvariant(),
            new ProviderDetails(profile.Name, profile.Kind, profile.ContactEmail, profile.Description),
            profile.ReviewNote, documents, branches.Select(branch => new BranchView(branch.Id,
                new BranchDetails(branch.Name, branch.Address, branch.City, branch.TimeZone,
                    JsonSerializer.Deserialize<OpeningWindow[]>(branch.HoursJson, Json)!, branch.ClosedUntil))).ToArray(), reviews);
    }

    private static Task<ProviderResult> Invalid() => Task.FromResult(new ProviderResult(ProviderResultStatus.Invalid));
}
