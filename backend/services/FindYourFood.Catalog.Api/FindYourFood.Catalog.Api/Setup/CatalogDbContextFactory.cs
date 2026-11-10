using FindYourFood.Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Catalog.Api.Setup;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = CatalogDbContext.OwnedConnection(configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Catalog before running migrations."));
        return new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connection).Options);
    }
}
