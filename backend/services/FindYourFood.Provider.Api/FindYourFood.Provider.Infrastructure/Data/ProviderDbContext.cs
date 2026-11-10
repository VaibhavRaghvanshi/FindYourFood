using FindYourFood.Provider.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Provider.Infrastructure.Data;

public sealed class ProviderDbContext(DbContextOptions<ProviderDbContext> options) : DbContext(options)
{
    public DbSet<ProviderProfile> Providers => Set<ProviderProfile>();
    public DbSet<VerificationDocument> Documents => Set<VerificationDocument>();
    public DbSet<ProviderBranch> Branches => Set<ProviderBranch>();
    public DbSet<ProviderReview> Reviews => Set<ProviderReview>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_ProviderDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_ProviderTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Provider requires its own FindYourFood_ProviderDb database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var provider = model.Entity<ProviderProfile>();
        provider.HasIndex(profile => profile.OwnerId).IsUnique();
        provider.HasIndex(profile => new { profile.Status, profile.UpdatedAt, profile.Id });
        provider.Property(profile => profile.Revision).IsConcurrencyToken();
        provider.Property(profile => profile.Name).HasMaxLength(100);
        provider.Property(profile => profile.Kind).HasMaxLength(24);
        provider.Property(profile => profile.ContactEmail).HasMaxLength(254);
        provider.Property(profile => profile.Description).HasMaxLength(400);
        provider.Property(profile => profile.ReviewNote).HasMaxLength(300);
        provider.Ignore(profile => profile.CanEditApplication);

        var documents = model.Entity<VerificationDocument>();
        documents.HasKey(document => new { document.ProviderId, document.Kind });
        documents.Property(document => document.Kind).HasMaxLength(20);
        documents.Property(document => document.MediaType).HasMaxLength(30);
        documents.HasOne<ProviderProfile>().WithMany().HasForeignKey(document => document.ProviderId);

        var branches = model.Entity<ProviderBranch>();
        branches.Property(branch => branch.Name).HasMaxLength(100);
        branches.Property(branch => branch.Address).HasMaxLength(240);
        branches.Property(branch => branch.City).HasMaxLength(80);
        branches.Property(branch => branch.TimeZone).HasMaxLength(80);
        branches.Property(branch => branch.HoursJson).HasMaxLength(600);
        branches.HasOne<ProviderProfile>().WithMany().HasForeignKey(branch => branch.ProviderId);

        var reviews = model.Entity<ProviderReview>();
        reviews.HasIndex(review => new { review.ProviderId, review.ApplicationRevision }).IsUnique();
        reviews.Property(review => review.Note).HasMaxLength(300);
        reviews.HasOne<ProviderProfile>().WithMany().HasForeignKey(review => review.ProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}
