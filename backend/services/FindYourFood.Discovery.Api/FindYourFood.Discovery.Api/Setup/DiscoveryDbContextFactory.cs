using FindYourFood.Discovery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Discovery.Api.Setup;

public sealed class DiscoveryDbContextFactory : IDesignTimeDbContextFactory<DiscoveryDbContext>
{
    public DiscoveryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = DiscoveryDbContext.OwnedConnection(configuration.GetConnectionString("Discovery")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Discovery before running migrations."));
        return new DiscoveryDbContext(new DbContextOptionsBuilder<DiscoveryDbContext>().UseSqlServer(connection).Options);
    }
}
