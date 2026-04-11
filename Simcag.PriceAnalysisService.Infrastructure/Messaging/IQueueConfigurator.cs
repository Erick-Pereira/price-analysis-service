using RabbitMQ.Client;

namespace Simcag.PriceAnalysisService.Infrastructure.Messaging;

public interface IQueueConfigurator
{
    string QueueName { get; }

    void ConfigureTopology(IModel channel);
}
