using FindYourFood.Location.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Location.Api.HealthChecks;

internal sealed class LocationReadiness(LocationDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Zones.AnyAsync(ct);
            return HealthCheckResult.Healthy();
    }
}
