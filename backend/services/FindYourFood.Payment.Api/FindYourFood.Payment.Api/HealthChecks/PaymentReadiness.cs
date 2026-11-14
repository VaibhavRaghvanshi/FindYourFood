using FindYourFood.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindYourFood.Payment.Api.HealthChecks;

internal sealed class PaymentReadiness(PaymentDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        await database.Intents.AnyAsync(ct);
        await database.Set<FindYourFood.Messaging.InboxRecord>().AnyAsync(ct);
        await database.Set<FindYourFood.Messaging.RejectedMessage>().AnyAsync(ct);
        return HealthCheckResult.Healthy();
    }
}
