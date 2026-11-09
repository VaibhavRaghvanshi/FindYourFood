using FindYourFood.Identity.Infrastructure;
using FindYourFood.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Identity.Api.Setup;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<IdentityDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Identity before running migrations.");
        var services = new ServiceCollection();
        services.AddIdentityPersistence(connection);
        return services.BuildServiceProvider().GetRequiredService<IdentityDbContext>();
    }
}
