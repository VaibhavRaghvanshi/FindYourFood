using FindYourFood.Catalog.Application.Interfaces;
using FindYourFood.Catalog.Domain;
using FindYourFood.Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Catalog.Infrastructure.Repositories;

internal sealed class MenuRepository(CatalogDbContext database) : IMenuRepository
{
    public Task<BranchMenu?> FindAsync(Guid branch, CancellationToken ct) =>
        database.Menus.SingleOrDefaultAsync(menu => menu.BranchId == branch, ct);

    public Task<MenuRelease?> PublishedAsync(Guid branch, CancellationToken ct) =>
        database.Releases.AsNoTracking().Where(release => release.BranchId == branch)
            .OrderByDescending(release => release.Version).FirstOrDefaultAsync(ct);

    public void Add(BranchMenu menu) => database.Menus.Add(menu);
    public void Add(MenuRelease release) => database.Releases.Add(release);
}
