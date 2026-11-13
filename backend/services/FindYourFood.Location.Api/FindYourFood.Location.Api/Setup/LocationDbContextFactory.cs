using FindYourFood.Location.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Location.Api.Setup;

public sealed class LocationDbContextFactory : IDesignTimeDbContextFactory<LocationDbContext>
{
    public LocationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = LocationDbContext.OwnedConnection(configuration.GetConnectionString("Location")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Location before running migrations."));
        return new LocationDbContext(new DbContextOptionsBuilder<LocationDbContext>().UseSqlServer(connection).Options);
    }
}
