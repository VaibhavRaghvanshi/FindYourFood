using FindYourFood.Experience.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Experience.Api.Setup;

public sealed class ExperienceDbContextFactory : IDesignTimeDbContextFactory<ExperienceDbContext>
{
    public ExperienceDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = ExperienceDbContext.OwnedConnection(configuration.GetConnectionString("Experience")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Experience before running migrations."));
        return new ExperienceDbContext(new DbContextOptionsBuilder<ExperienceDbContext>().UseSqlServer(connection).Options);
    }
}
