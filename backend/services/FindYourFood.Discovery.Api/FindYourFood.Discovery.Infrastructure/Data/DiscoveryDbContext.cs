using FindYourFood.Discovery.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Discovery.Infrastructure.Data;

public sealed class DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options) : DbContext(options)
{
    public DbSet<CityIndex> Indexes => Set<CityIndex>();
    public DbSet<BranchProjection> Projections => Set<BranchProjection>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_DiscoveryDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_DiscoveryTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Discovery requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var index = model.Entity<CityIndex>();
        index.HasKey(row => row.City);
        index.Property(row => row.City).HasMaxLength(80);
        index.Property(row => row.Revision).IsConcurrencyToken();
        var projection = model.Entity<BranchProjection>();
        projection.HasKey(row => new { row.City, row.BranchId });
        projection.Property(row => row.City).HasMaxLength(80);
    }
}
