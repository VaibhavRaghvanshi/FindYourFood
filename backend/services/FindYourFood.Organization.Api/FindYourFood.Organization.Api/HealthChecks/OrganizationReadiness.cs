using FindYourFood.Organization.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Organization.Api.HealthChecks;

internal sealed class OrganizationReadiness(OrganizationDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await database.ProbeGrantsAsync(cancellationToken);
        await database.ProbeAuditAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
