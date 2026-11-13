using FindYourFood.Location.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Location.Infrastructure.Data;

public sealed class LocationDbContext(DbContextOptions<LocationDbContext> options) : DbContext(options)
{
    public DbSet<DeliveryZone> Zones => Set<DeliveryZone>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_LocationDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_LocationTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Location requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var zone = model.Entity<DeliveryZone>();
        zone.HasKey(row => row.Id);
        zone.Property(row => row.Id).ValueGeneratedNever();
        zone.Property(row => row.Revision).IsConcurrencyToken();
        zone.Property(row => row.Name).HasMaxLength(100);
        zone.Property(row => row.City).HasMaxLength(100);
        zone.HasIndex(row => new { row.Enabled, row.Latitude });
    }
}
