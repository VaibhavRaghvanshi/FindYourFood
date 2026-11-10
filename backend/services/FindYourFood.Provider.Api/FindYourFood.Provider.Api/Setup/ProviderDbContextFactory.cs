using FindYourFood.Provider.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Provider.Api.Setup;

public sealed class ProviderDbContextFactory : IDesignTimeDbContextFactory<ProviderDbContext>
{
    public ProviderDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = ProviderDbContext.OwnedConnection(configuration.GetConnectionString("Provider")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Provider before running migrations."));
        return new ProviderDbContext(new DbContextOptionsBuilder<ProviderDbContext>().UseSqlServer(connection).Options);
    }
}
