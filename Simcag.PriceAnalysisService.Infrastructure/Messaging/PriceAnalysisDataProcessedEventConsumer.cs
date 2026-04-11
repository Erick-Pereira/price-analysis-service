using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Infrastructure.Messaging;

public sealed class PriceAnalysisDataProcessedEventConsumer : IEventConsumer<DataProcessedEvent>, IDisposable
{
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly ConcurrentQueue<MessageEnvelope<DataProcessedEvent>> _messageQueue = new();
    private readonly ConcurrentDictionary<ulong, MessageEnvelope<DataProcessedEvent>> _pendingAcks = new();
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly EventingBasicConsumer _consumer;
    private bool _disposed;

    public PriceAnalysisDataProcessedEventConsumer(
        IRabbitMqChannelFactory channelFactory,
        IQueueConfigurator queueConfigurator,
        RabbitMqOptions options)
    {
        _queueName = queueConfigurator.QueueName;
        _channel = channelFactory.CreateChannel();

        queueConfigurator.ConfigureTopology(_channel);
        _channel.BasicQos(0, options.PrefetchCount, false);

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += OnMessageReceived;

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: options.AutoAck,
            consumerTag: typeof(DataProcessedEvent).Name,
            consumer: _consumer);
    }

    private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var envelope = JsonSerializer.Deserialize<MessageEnvelope<DataProcessedEvent>>(json);
            if (envelope != null)
            {
                _pendingAcks.TryAdd(ea.DeliveryTag, envelope);
                _messageQueue.Enqueue(envelope);
                _semaphore.Release();
            }
            else
            {
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }
        catch
        {
            _channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    public async IAsyncEnumerable<MessageEnvelope<DataProcessedEvent>> ReadMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_messageQueue.TryDequeue(out var message))
            {
                yield return message;
            }
        }
    }

    public Task AcknowledgeMessageAsync(MessageEnvelope<DataProcessedEvent> message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var pending = _pendingAcks.FirstOrDefault(x => x.Value.Data.EventId == message.Data.EventId);
        if (pending.Key != 0)
        {
            _pendingAcks.TryRemove(pending.Key, out _);
            _channel.BasicAck(pending.Key, false);
        }

        return Task.CompletedTask;
    }

    public Task RejectMessageAsync(MessageEnvelope<DataProcessedEvent> message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var pending = _pendingAcks.FirstOrDefault(x => x.Value.Data.EventId == message.Data.EventId);
        if (pending.Key != 0)
        {
            _pendingAcks.TryRemove(pending.Key, out _);
            _channel.BasicNack(pending.Key, false, false);
        }

        return Task.CompletedTask;
    }

    public Task CreateConsumerGroupIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _channel?.Dispose();
        _semaphore?.Dispose();
    }
}
