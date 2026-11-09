using FindYourFood.Organization.Application;
using FindYourFood.Organization.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Organization.Infrastructure;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : DbContext(options)
{
    internal DbSet<AccessGrant> Grants => Set<AccessGrant>();
    internal DbSet<GrantAudit> Audit => Set<GrantAudit>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_OrganizationDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_OrganizationTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Organization requires its own FindYourFood_OrganizationDb database.");
        }

        return connection;
    }

    public Task<bool> ProbeGrantsAsync(CancellationToken cancellationToken) => Grants.AnyAsync(cancellationToken);
    public Task<bool> ProbeAuditAsync(CancellationToken cancellationToken) => Audit.AnyAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder model)
    {
        var grants = model.Entity<AccessGrant>();
        grants.Property(grant => grant.Permission).HasMaxLength(64).IsUnicode(false);
        grants.Property(grant => grant.Scope).HasMaxLength(160).IsUnicode(false);
        grants.HasIndex(grant => new { grant.SubjectId, grant.Permission, grant.Scope })
            .IsUnique().HasFilter("[RevokedAt] IS NULL");
        grants.HasIndex(grant => new { grant.GrantedBy, grant.GrantedAt });
        grants.HasIndex(grant => grant.GrantedAt);
        grants.HasOne<AccessGrant>().WithMany().HasForeignKey(grant => grant.ParentGrantId)
            .OnDelete(DeleteBehavior.Restrict);

        var audit = model.Entity<GrantAudit>();
        audit.Property(entry => entry.Action).HasMaxLength(16).IsUnicode(false);
        audit.Property(entry => entry.TraceId).HasMaxLength(64).IsUnicode(false);
        audit.HasOne<AccessGrant>().WithMany().HasForeignKey(entry => entry.GrantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
