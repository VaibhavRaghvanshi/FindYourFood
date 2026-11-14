using FindYourFood.Contracts;
using RabbitMQ.Client;

namespace FindYourFood.Messaging;

public sealed class RabbitPublisher(BrokerSettings settings) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(IntegrationEvent message, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_channel?.IsOpen != true)
            {
                await CloseAsync();
                _connection = await settings.Factory().CreateConnectionAsync(settings.Service + "-publisher", ct);
                _channel = await _connection.CreateChannelAsync(
                    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
                await _channel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Topic, durable: true,
                    autoDelete: false, cancellationToken: ct);
            }
            var properties = new BasicProperties
            {
                Persistent = true, ContentType = "application/json", MessageId = message.MessageId.ToString("D"),
                Type = message.Type, UserId = settings.User
            };
            await _channel.BasicPublishAsync(settings.Exchange, message.Type, mandatory: true,
                basicProperties: properties, body: body, cancellationToken: ct);
        }
        catch
        {
            await CloseAsync();
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task CloseAsync()
    {
        if (_channel is not null) { await _channel.DisposeAsync(); _channel = null; }
        if (_connection is not null) { await _connection.DisposeAsync(); _connection = null; }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _gate.Dispose();
    }
}
