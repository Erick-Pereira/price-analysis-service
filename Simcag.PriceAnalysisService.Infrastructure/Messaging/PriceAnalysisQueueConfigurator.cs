using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.Shared.Messaging.Configuration;

namespace Simcag.PriceAnalysisService.Infrastructure.Messaging;

public sealed class PriceAnalysisQueueConfigurator : IQueueConfigurator
{
    private readonly RabbitMqOptions _options;
    private const string ExchangeName = "simcag-events";
    private const string QueueNameValue = "price-analysis-service";

    public PriceAnalysisQueueConfigurator(RabbitMqOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string QueueName => QueueNameValue;

    public void ConfigureTopology(IModel channel)
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel));

        channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null);

        var arguments = new Dictionary<string, object>();
        if (_options.EnableDeadLetterQueue)
        {
            var deadLetterQueueName = GetDeadLetterQueueName(QueueNameValue);
            arguments["x-dead-letter-exchange"] = string.Empty;
            arguments["x-dead-letter-routing-key"] = deadLetterQueueName;
            arguments["x-message-ttl"] = _options.DeadLetterTtlMilliseconds;
        }

        channel.QueueDeclare(
            queue: QueueNameValue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments.Count > 0 ? arguments : null);

        channel.QueueBind(
            queue: QueueNameValue,
            exchange: ExchangeName,
            routingKey: typeof(DataProcessedEvent).Name);

        if (_options.EnableDeadLetterQueue)
        {
            channel.QueueDeclare(
                queue: GetDeadLetterQueueName(QueueNameValue),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }
    }

    private static string GetDeadLetterQueueName(string queueName) => $"{queueName}.dlq";
}
