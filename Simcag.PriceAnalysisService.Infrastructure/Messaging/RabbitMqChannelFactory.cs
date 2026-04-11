using RabbitMQ.Client;
using Simcag.Shared.Messaging.Configuration;

namespace Simcag.PriceAnalysisService.Infrastructure.Messaging;

public sealed class RabbitMqChannelFactory : IRabbitMqChannelFactory, IDisposable
{
    private readonly IConnection _connection;
    private bool _disposed;

    public RabbitMqChannelFactory(RabbitMqOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryIntervalSeconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeatSeconds),
            ContinuationTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds),
            DispatchConsumersAsync = false
        };

        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _connection.Dispose();
    }
}
