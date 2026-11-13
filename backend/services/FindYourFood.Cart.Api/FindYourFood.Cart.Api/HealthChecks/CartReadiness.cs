using FindYourFood.Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Cart.Api.HealthChecks;

internal sealed class CartReadiness(CartDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Carts.AnyAsync(ct);
            return HealthCheckResult.Healthy();
    }
}
