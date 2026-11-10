using System.Text.Json;
using FindYourFood.Catalog.Application.DTOs;
using FindYourFood.Catalog.Application.Interfaces;
using FindYourFood.Catalog.Domain;

namespace FindYourFood.Catalog.Application.Services;

public sealed class MenuPublishing(IMenuRepository repository, IUnitOfWork unitOfWork, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<MenuWorkspace> WorkspaceAsync(Guid branch, CancellationToken ct) =>
        View(await repository.FindAsync(branch, ct) ?? new BranchMenu(branch));

    public async Task<PublishedMenu?> PublishedAsync(Guid branch, CancellationToken ct)
    {
        var release = await repository.PublishedAsync(branch, ct);
        return release is null ? null : new PublishedMenu(branch, release.Version, "INR", Read(release.ContentJson));
    }

    public async Task<MenuResult> SaveDraftAsync(Guid branch, MenuDraft request, CancellationToken ct)
    {
        if (!MenuRules.IsValid(request.Menu)) return new MenuResult(MenuStatus.Invalid);
        var current = await repository.FindAsync(branch, ct);
        if ((current?.Revision ?? 0) != request.ExpectedRevision) return new MenuResult(MenuStatus.Conflict);
        var workspace = current ?? new BranchMenu(branch);
        if (current is null) repository.Add(workspace);

        workspace.Save(JsonSerializer.Serialize(request.Menu, Json));
        return await SaveAsync(workspace, ct);
    }

    public async Task<MenuResult> PublishAsync(Guid branch, PublishMenu request, CancellationToken ct)
    {
        var workspace = await repository.FindAsync(branch, ct);
        if (workspace is null || workspace.Revision != request.ExpectedRevision || !workspace.HasUnpublishedChanges)
        {
            return new MenuResult(MenuStatus.Conflict);
        }

        repository.Add(workspace.Publish(clock.GetUtcNow()));
        return await SaveAsync(workspace, ct);
    }

    private async Task<MenuResult> SaveAsync(BranchMenu workspace, CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveAsync(ct);
            return new MenuResult(MenuStatus.Accepted, View(workspace));
        }
        catch (MenuConflictException)
        {
            return new MenuResult(MenuStatus.Conflict);
        }
    }

    private static MenuWorkspace View(BranchMenu workspace) => new(workspace.Revision, workspace.PublishedVersion,
        workspace.HasUnpublishedChanges, workspace.DraftJson is null ? null : Read(workspace.DraftJson));

    private static MenuContent Read(string value) => JsonSerializer.Deserialize<MenuContent>(value, Json)!;
}
