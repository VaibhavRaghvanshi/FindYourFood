using FindYourFood.Experience.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Experience.Infrastructure.Data;

public sealed class ExperienceDbContext(DbContextOptions<ExperienceDbContext> options) : DbContext(options)
{
    public DbSet<ExperienceWorkspace> Workspaces => Set<ExperienceWorkspace>();
    public DbSet<ExperiencePublication> Publications => Set<ExperiencePublication>();

    public static string OwnedConnection(string connection)
    {
        var catalog = new SqlConnectionStringBuilder(connection).InitialCatalog;
        if (!string.Equals(catalog, "FindYourFood_ExperienceDb", StringComparison.OrdinalIgnoreCase) &&
            !catalog.StartsWith("FindYourFood_ExperienceTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Experience requires its own FindYourFood_ExperienceDb database.");
        }

        return connection;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var workspace = model.Entity<ExperienceWorkspace>();
        workspace.Property(state => state.Id).ValueGeneratedNever();
        workspace.Property(state => state.Revision).IsConcurrencyToken();
        workspace.HasData(new
        {
            Id = ExperienceWorkspace.CustomerHomeId,
            Revision = 0,
            PublishedVersion = 0,
            DraftPublished = false
        });

        var publication = model.Entity<ExperiencePublication>();
        publication.HasKey(release => release.Version);
        publication.Property(release => release.Version).ValueGeneratedNever();
    }
}
