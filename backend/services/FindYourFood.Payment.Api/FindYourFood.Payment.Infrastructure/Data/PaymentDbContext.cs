using FindYourFood.Payment.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Payment.Infrastructure.Data;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentIntent> Intents => Set<PaymentIntent>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_PaymentDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_PaymentTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Payment requires its own database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var intent = model.Entity<PaymentIntent>();
        intent.HasKey(row => row.Id);
        intent.Property(row => row.Id).ValueGeneratedNever();
        intent.HasIndex(row => row.OrderId).IsUnique();
        intent.Property(row => row.Revision).IsConcurrencyToken();
        intent.Property(row => row.Currency).HasMaxLength(3).IsUnicode(false);
        intent.Property(row => row.GuestKeyHash).HasMaxLength(64).IsUnicode(false);
        var attempt = model.Entity<PaymentAttempt>();
        attempt.HasKey(row => row.Id);
        attempt.Property(row => row.Id).ValueGeneratedNever();
        attempt.Property(row => row.Revision).IsConcurrencyToken();
        attempt.Property(row => row.ProviderReference).HasMaxLength(100);
        attempt.HasIndex(row => new { row.IntentId, row.IdempotencyKey }).IsUnique();
        attempt.HasIndex(row => new { row.State, row.NextRetryAt });
        attempt.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => row.IntentId).OnDelete(DeleteBehavior.Restrict);
        var refund = model.Entity<RefundInstruction>();
        refund.HasKey(row => row.Id);
        refund.Property(row => row.Id).ValueGeneratedNever();
        refund.Property(row => row.Revision).IsConcurrencyToken();
        refund.Property(row => row.Currency).HasMaxLength(3).IsUnicode(false);
        refund.Property(row => row.ProviderReference).HasMaxLength(100);
        refund.HasIndex(row => row.PaymentId).IsUnique();
        refund.HasIndex(row => new { row.Completed, row.NextRetryAt });
        refund.HasOne<PaymentAttempt>().WithMany().HasForeignKey(row => row.PaymentId).OnDelete(DeleteBehavior.Restrict);
        var cancellation = model.Entity<CancelledOrder>();
        cancellation.HasKey(row => row.OrderId);
        cancellation.Property(row => row.OrderId).ValueGeneratedNever();
        FindYourFood.Messaging.MessagingModel.Configure(model);
    }
}
