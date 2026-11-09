using FindYourFood.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Organization.Api;

public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = OrganizationDbContext.OwnedConnection(configuration.GetConnectionString("Organization")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Organization before running migrations."));
        return new OrganizationDbContext(new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseSqlServer(connection).Options);
    }
}
