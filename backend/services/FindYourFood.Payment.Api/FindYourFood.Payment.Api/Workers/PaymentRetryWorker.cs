using FindYourFood.Payment.Application.Services;

namespace FindYourFood.Payment.Api.Workers;

internal sealed class PaymentRetryWorker(IServiceScopeFactory scopes, ILogger<PaymentRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Application.DTOs.PendingPaymentWork pending;
                using (var scope = scopes.CreateScope())
                    pending = await scope.ServiceProvider.GetRequiredService<Payments>().PendingAsync(stoppingToken);
                foreach (var payment in pending.Payments) await Process(payment, false, stoppingToken);
                foreach (var refund in pending.Refunds) await Process(refund, true, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning("Payment reconciliation scan will retry ({ErrorType}).", error.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
    private async Task Process(Guid id, bool refund, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var payments = scope.ServiceProvider.GetRequiredService<Payments>();
            if (refund) await payments.ProcessRefundAsync(id, ct);
            else await payments.ProcessAttemptAsync(id, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error) { logger.LogWarning("Payment work {WorkId} will retry ({ErrorType}).", id, error.GetType().Name); }
    }
}
