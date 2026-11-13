using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Application.Interfaces;
using FindYourFood.Provider.Domain;
using FindYourFood.Provider.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FindYourFood.Provider.Infrastructure.Repositories;

internal sealed class ProviderRepository(ProviderDbContext database) : IProviderRepository
{
    public async Task<IReadOnlyList<PublicBranchView>> PublicAsync(string? city, Guid? id, int page, CancellationToken ct)
    {
        var query = from branch in database.Branches.AsNoTracking()
                    join provider in database.Providers.AsNoTracking() on branch.ProviderId equals provider.Id
                    where provider.Status == ProviderStatus.Approved
                    select new { Branch = branch, ProviderName = provider.Name, provider.Kind };
        if (city is not null) query = query.Where(row =>
            EF.Functions.Collate(row.Branch.City, "Latin1_General_100_CI_AS") == city);
        if (id is not null) query = query.Where(row => row.Branch.Id == id);
        var rows = await query.OrderBy(row => row.Branch.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return rows.Select(row => new PublicBranchView(row.Branch.Id, row.ProviderName, row.Kind,
            new BranchDetails(row.Branch.Name, row.Branch.Address, row.Branch.City, row.Branch.TimeZone,
                JsonSerializer.Deserialize<OpeningWindow[]>(row.Branch.HoursJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))!, row.Branch.ClosedUntil,
                row.Branch.Latitude, row.Branch.Longitude))).ToArray();
    }

    public Task<ProviderProfile?> FindOwnedAsync(Guid owner, CancellationToken ct) =>
        database.Providers.SingleOrDefaultAsync(profile => profile.OwnerId == owner, ct);

    public Task<ProviderProfile?> FindAsync(Guid id, CancellationToken ct) =>
        database.Providers.SingleOrDefaultAsync(profile => profile.Id == id, ct);

    public async Task<IReadOnlyList<ProviderSummary>> PendingAsync(CancellationToken ct) =>
        await database.Providers.AsNoTracking().Where(profile => profile.Status == ProviderStatus.Submitted)
            .OrderBy(profile => profile.UpdatedAt).ThenBy(profile => profile.Id).Take(50)
            .Select(profile => new ProviderSummary(profile.Id, profile.Name, profile.Kind, profile.UpdatedAt)).ToListAsync(ct);

    public async Task<IReadOnlyList<DocumentView>> DocumentsAsync(Guid provider, CancellationToken ct) =>
        await database.Documents.AsNoTracking().Where(document => document.ProviderId == provider)
            .OrderBy(document => document.Kind)
            .Select(document => new DocumentView(document.Kind, document.MediaType, document.Size, document.UploadedAt))
            .ToListAsync(ct);

    public Task<VerificationDocument?> DocumentAsync(Guid provider, string kind, CancellationToken ct) =>
        database.Documents.SingleOrDefaultAsync(document => document.ProviderId == provider && document.Kind == kind, ct);

    public async Task<IReadOnlyList<ProviderBranch>> BranchesAsync(Guid provider, CancellationToken ct) =>
        await database.Branches.AsNoTracking().Where(branch => branch.ProviderId == provider)
            .OrderBy(branch => branch.Name).ThenBy(branch => branch.Id).Take(ProviderRules.MaxBranches).ToListAsync(ct);

    public Task<ProviderBranch?> BranchAsync(Guid provider, Guid branch, CancellationToken ct) =>
        database.Branches.SingleOrDefaultAsync(item => item.ProviderId == provider && item.Id == branch, ct);

    public async Task<IReadOnlyList<ProviderReview>> ReviewsAsync(Guid provider, CancellationToken ct) =>
        await database.Reviews.AsNoTracking().Where(review => review.ProviderId == provider)
            .OrderByDescending(review => review.ApplicationRevision).Take(20).ToListAsync(ct);

    public void Add(ProviderProfile provider) => database.Providers.Add(provider);
    public void Add(VerificationDocument document) => database.Documents.Add(document);
    public void Add(ProviderBranch branch) => database.Branches.Add(branch);
    public void Add(ProviderReview review) => database.Reviews.Add(review);
}
