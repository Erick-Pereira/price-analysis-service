using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Simcag.ProcessingService.Domain.Events;
using Simcag.Shared.Messaging.Contracts;
using Simcag.PriceAnalysisService.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Events;

namespace Simcag.PriceAnalysisService.Application.Workers;

public class DataProcessedEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataProcessedEventConsumer> _logger;
    private readonly IEventConsumer<DataProcessedEvent> _eventConsumer;

    public DataProcessedEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<DataProcessedEventConsumer> logger,
        IEventConsumer<DataProcessedEvent> eventConsumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventConsumer = eventConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting DataProcessedEvent consumer");

        await foreach (var messageEnvelope in _eventConsumer.ReadMessagesAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var priceAnalysisService = scope.ServiceProvider.GetRequiredService<IPriceAnalysisService>();

            try
            {
                await priceAnalysisService.AnalyzePriceAsync(messageEnvelope.Data, stoppingToken);
                await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
                _logger.LogInformation("Successfully processed DataProcessedEvent for product {ProductId}",
                    messageEnvelope.Data.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DataProcessedEvent for product {ProductId}",
                    messageEnvelope.Data.ProductId);
                await _eventConsumer.RejectMessageAsync(messageEnvelope, stoppingToken);
            }
        }

        _logger.LogInformation("DataProcessedEvent consumer stopped");
    }
}