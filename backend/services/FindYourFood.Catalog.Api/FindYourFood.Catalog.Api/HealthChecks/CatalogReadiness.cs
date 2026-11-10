using FindYourFood.Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Catalog.Api.HealthChecks;

internal sealed class CatalogReadiness(CatalogDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Menus.AnyAsync(ct);
        await database.Releases.AnyAsync(ct);
        return HealthCheckResult.Healthy();
    }
}
