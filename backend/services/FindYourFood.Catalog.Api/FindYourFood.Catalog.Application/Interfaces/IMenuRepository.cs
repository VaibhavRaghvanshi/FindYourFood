using FindYourFood.Catalog.Domain;

namespace FindYourFood.Catalog.Application.Interfaces;

public interface IMenuRepository
{
    Task<BranchMenu?> FindAsync(Guid branch, CancellationToken ct);
    Task<MenuRelease?> PublishedAsync(Guid branch, CancellationToken ct);
    void Add(BranchMenu menu);
    void Add(MenuRelease release);
}
