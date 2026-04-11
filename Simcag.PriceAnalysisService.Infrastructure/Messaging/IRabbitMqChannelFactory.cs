using RabbitMQ.Client;

namespace Simcag.PriceAnalysisService.Infrastructure.Messaging;

public interface IRabbitMqChannelFactory
{
    IModel CreateChannel();
}
