using FindYourFood.Provider.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Provider.Api.HealthChecks;

internal sealed class ProviderReadiness(ProviderDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        await database.Providers.AnyAsync(cancellationToken);
        await database.Documents.AnyAsync(cancellationToken);
        await database.Branches.AnyAsync(cancellationToken);
        await database.Reviews.AnyAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
