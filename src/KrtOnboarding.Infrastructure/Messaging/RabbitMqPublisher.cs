using System.Text.Json;
using KrtOnboarding.Application.Messaging;
using RabbitMQ.Client;

namespace KrtOnboarding.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;

    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(
        string hostName,
        int port,
        string userName,
        string password,
        string virtualHost)
    {
        _connectionFactory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            VirtualHost = virtualHost
        };
    }

    public async Task PublishAsync<T>(T message, string exchangeName) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        await _publishLock.WaitAsync();
        try
        {
            await EnsureChannelAsync(exchangeName);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task EnsureChannelAsync(string exchangeName)
    {
        if (_channel is not null)
            return;

        var connection = await _connectionFactory.CreateConnectionAsync();
        IChannel? channel = null;
        try
        {
            channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null);

            _connection = connection;
            _channel = channel;
        }
        catch
        {
            if (channel is not null)
                await channel.DisposeAsync();

            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        _publishLock.Dispose();
    }
}