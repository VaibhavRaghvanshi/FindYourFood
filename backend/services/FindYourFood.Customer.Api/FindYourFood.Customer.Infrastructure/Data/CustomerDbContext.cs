using FindYourFood.Customer.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Customer.Infrastructure.Data;

public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<CustomerProfile> Profiles => Set<CustomerProfile>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_CustomerDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_CustomerTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Customer requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var profile = model.Entity<CustomerProfile>();
        profile.HasKey(row => row.CustomerId);
        profile.Property(row => row.CustomerId).ValueGeneratedNever();
        profile.Property(row => row.Revision).IsConcurrencyToken();
        profile.Property(row => row.SettingsJson).IsRequired();
    }
}
