using FindYourFood.Cart.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Cart.Infrastructure.Data;

public sealed class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_CartDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_CartTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cart requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var cart = model.Entity<ShoppingCart>();
        cart.HasKey(row => row.Id);
        cart.Property(row => row.Id).ValueGeneratedNever();
        cart.Property(row => row.Revision).IsConcurrencyToken();
        cart.Property(row => row.GuestKeyHash).HasMaxLength(64).IsUnicode(false);
        cart.HasIndex(row => new { row.CustomerId, row.ExpiresAt });
    }
}
