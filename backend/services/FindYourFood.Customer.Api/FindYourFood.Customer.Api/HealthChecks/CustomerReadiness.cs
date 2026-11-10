using FindYourFood.Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Customer.Api.HealthChecks;

internal sealed class CustomerReadiness(CustomerDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Profiles.AnyAsync(ct);
            return HealthCheckResult.Healthy();
    }
}
