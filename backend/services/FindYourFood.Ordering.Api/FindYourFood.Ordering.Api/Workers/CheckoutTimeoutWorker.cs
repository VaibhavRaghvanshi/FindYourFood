using FindYourFood.Ordering.Application.Services;

namespace FindYourFood.Ordering.Api.Workers;

internal sealed class CheckoutTimeoutWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<CheckoutTimeoutWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OrderCheckout>().ExpireAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning(error, "Checkout timeout processing will retry."); }
            await Task.Delay(TimeSpan.FromSeconds(30), clock, stoppingToken);
        }
    }
}
