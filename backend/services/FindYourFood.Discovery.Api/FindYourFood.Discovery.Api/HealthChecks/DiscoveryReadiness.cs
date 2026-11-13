using FindYourFood.Discovery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Discovery.Api.HealthChecks;

internal sealed class DiscoveryReadiness(DiscoveryDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Indexes.AnyAsync(ct);
            return HealthCheckResult.Healthy();
    }
}
