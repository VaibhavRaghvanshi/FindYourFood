using System.Text.Json;
using FindYourFood.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FindYourFood.Messaging;

internal sealed class OutboxDispatcher(BrokerSettings settings, RabbitPublisher publisher, IServiceScopeFactory scopes,
    TimeProvider clock, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IMessageStore>();
                var lease = Guid.NewGuid();
                var row = await store.ClaimAsync(lease, clock.GetUtcNow(), stoppingToken);
                if (row is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), clock, stoppingToken);
                    continue;
                }
                try
                {
                    using var payload = JsonDocument.Parse(row.PayloadJson);
                    var message = new IntegrationEvent(row.Id, row.Source, row.Type, row.AggregateId,
                        row.AggregateVersion, row.OccurredAt, payload.RootElement.Clone());
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(message, IntegrationEvent.Json);
                    if (!message.IsValid() || bytes.Length > 65536)
                    {
                        await store.RetryAsync(row, lease, clock.GetUtcNow(), "invalid-envelope", true, stoppingToken);
                        continue;
                    }
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    deadline.CancelAfter(TimeSpan.FromSeconds(10));
                    await publisher.PublishAsync(message, bytes, deadline.Token);
                    await store.CompleteAsync(row.Id, lease, clock.GetUtcNow(), stoppingToken);
                }
                catch (Exception error) when (!stoppingToken.IsCancellationRequested)
                {
                    await store.RetryAsync(row, lease, clock.GetUtcNow(), error.GetType().Name,
                        error is JsonException, stoppingToken);
                    logger.LogWarning("Outbox message {MessageType} {MessageId} will retry after {ErrorType}.", row.Type, row.Id, error.GetType().Name);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                logger.LogWarning("Outbox scan will retry after {ErrorType}.", error.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(2), clock, stoppingToken);
            }
        }
    }
}
