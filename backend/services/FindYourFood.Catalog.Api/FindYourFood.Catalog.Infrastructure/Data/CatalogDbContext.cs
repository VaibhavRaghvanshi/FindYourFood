using FindYourFood.Catalog.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Catalog.Infrastructure.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<BranchMenu> Menus => Set<BranchMenu>();
    public DbSet<MenuRelease> Releases => Set<MenuRelease>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_CatalogDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_CatalogTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Catalog requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var menu = model.Entity<BranchMenu>();
        menu.HasKey(item => item.BranchId);
        menu.Property(item => item.BranchId).ValueGeneratedNever();
        menu.Property(item => item.Revision).IsConcurrencyToken();

        var release = model.Entity<MenuRelease>();
        release.HasKey(item => new { item.BranchId, item.Version });
        release.HasOne<BranchMenu>().WithMany().HasForeignKey(item => item.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
