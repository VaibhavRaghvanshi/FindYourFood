using FindYourFood.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Identity.Api;

internal sealed class IdentityReadiness(IdentityDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await db.Users.AnyAsync(cancellationToken);
        await db.PrivilegedEnrollments.AnyAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
