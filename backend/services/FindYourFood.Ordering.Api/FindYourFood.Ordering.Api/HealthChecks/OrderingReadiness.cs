using FindYourFood.Ordering.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FindYourFood.Messaging;

namespace FindYourFood.Ordering.Api.HealthChecks;

internal sealed class OrderingReadiness(OrderingDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Orders.AnyAsync(ct);
            await database.Sagas.AnyAsync(ct);
        await database.Outbox.AnyAsync(ct);
        await database.Set<InboxRecord>().AnyAsync(ct);
        await database.Set<RejectedMessage>().AnyAsync(ct);
        return HealthCheckResult.Healthy();
    }
}
