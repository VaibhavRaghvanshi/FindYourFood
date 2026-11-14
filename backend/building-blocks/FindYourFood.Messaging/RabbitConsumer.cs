using System.Text.Json;
using FindYourFood.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FindYourFood.Messaging;

internal sealed class RabbitConsumer(BrokerSettings settings, IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<RabbitConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled || settings.Bindings.Length == 0) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await settings.Factory().CreateConnectionAsync(settings.Service + "-consumer", stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                connection.ConnectionShutdownAsync += (_, _) => { disconnected.TrySetResult(); return Task.CompletedTask; };
                channel.ChannelShutdownAsync += (_, _) => { disconnected.TrySetResult(); return Task.CompletedTask; };
                await channel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Topic, durable: true,
                    autoDelete: false, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(settings.Queue, durable: true, exclusive: false, autoDelete: false,
                    cancellationToken: stoppingToken);
                foreach (var binding in settings.Bindings)
                {
                    await channel.QueueBindAsync(settings.Queue, settings.Exchange, binding, cancellationToken: stoppingToken);
                }
                await channel.BasicQosAsync(0, 1, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    var bytes = delivery.Body.ToArray();
                    try
                    {
                        if (bytes.Length > 65536) throw new InvalidIntegrationEventException("message-too-large");
                        var message = JsonSerializer.Deserialize<IntegrationEvent>(bytes, IntegrationEvent.Json)
                            ?? throw new InvalidIntegrationEventException("missing-envelope");
                        if (!message.IsValid() || delivery.RoutingKey != message.Type)
                        {
                            throw new InvalidIntegrationEventException("invalid-envelope");
                        }
                        await using var scope = scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler>().HandleAsync(message, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception error) when (error is InvalidIntegrationEventException or JsonException)
                    {
                        try
                        {
                            await using var scope = scopes.CreateAsyncScope();
                            await scope.ServiceProvider.GetRequiredService<IMessageStore>()
                                .RejectAsync(bytes, error is JsonException ? "invalid-json" : error.Message, clock.GetUtcNow(), stoppingToken);
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                        }
                        catch (Exception storageError) when (!stoppingToken.IsCancellationRequested)
                        {
                            await RetryAsync(channel, delivery.DeliveryTag, storageError, stoppingToken);
                        }
                    }
                    catch (Exception error) when (!stoppingToken.IsCancellationRequested)
                    {
                        await RetryAsync(channel, delivery.DeliveryTag, error, stoppingToken);
                    }
                };
                await channel.BasicConsumeAsync(settings.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                await disconnected.Task.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning("Consumer connection will retry after {ErrorType}.", error.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(2), clock, stoppingToken);
        }
    }

    private async Task RetryAsync(IChannel channel, ulong tag, Exception error, CancellationToken ct)
    {
        logger.LogWarning("Message delivery will retry after {ErrorType}.", error.GetType().Name);
        await Task.Delay(TimeSpan.FromSeconds(2), clock, ct);
        if (channel.IsOpen) await channel.BasicNackAsync(tag, false, true, ct);
    }
}
