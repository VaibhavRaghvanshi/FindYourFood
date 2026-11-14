using FindYourFood.Ordering.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using FindYourFood.Messaging;

namespace FindYourFood.Ordering.Infrastructure.Data;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<CheckoutSaga> Sagas => Set<CheckoutSaga>();
    public DbSet<OutboxRecord> Outbox => Set<OutboxRecord>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_OrderingDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_OrderingTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Ordering requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var order = model.Entity<Order>();
        order.HasKey(row => row.Id);
        order.Property(row => row.Id).ValueGeneratedNever();
        order.Property(row => row.Revision).IsConcurrencyToken();
        order.Property(row => row.OwnerScope).HasMaxLength(80).IsUnicode(false);
        order.Property(row => row.GuestKeyHash).HasMaxLength(64).IsUnicode(false);
        order.Property(row => row.RequestHash).HasMaxLength(64).IsUnicode(false);
        order.HasIndex(row => new { row.OwnerScope, row.IdempotencyKey }).IsUnique();
        order.HasIndex(row => new { row.CartId, row.CartRevision }).IsUnique();
        order.HasIndex(row => new { row.OwnerScope, row.CreatedAt });
        var saga = model.Entity<CheckoutSaga>();
        saga.HasKey(row => row.OrderId);
        saga.Property(row => row.OrderId).ValueGeneratedNever();
        saga.Property(row => row.Revision).IsConcurrencyToken();
        saga.HasIndex(row => new { row.State, row.PaymentDeadline });
        saga.HasOne<Order>().WithOne().HasForeignKey<CheckoutSaga>(row => row.OrderId).OnDelete(DeleteBehavior.Restrict);
        MessagingModel.Configure(model);
        var message = model.Entity<OutboxRecord>();
        message.Property(row => row.Source).HasDefaultValue("ordering");
        message.HasOne<Order>().WithMany().HasForeignKey(row => row.AggregateId).OnDelete(DeleteBehavior.Restrict);
    }
}
