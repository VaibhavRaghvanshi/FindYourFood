using FindYourFood.Experience.Domain;
using FindYourFood.Experience.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Experience.Api.HealthChecks;

internal sealed class ExperienceReadiness(ExperienceDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        if (!await database.Workspaces.AnyAsync(state => state.Id == ExperienceWorkspace.CustomerHomeId, cancellationToken))
        {
            return HealthCheckResult.Unhealthy("The customer workspace has not been initialized.");
        }

        await database.Publications.AnyAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
