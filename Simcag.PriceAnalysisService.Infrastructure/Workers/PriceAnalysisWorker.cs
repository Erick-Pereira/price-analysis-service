using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.PriceAnalysisService.Application.UseCases;
using Simcag.Shared.Messaging.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Infrastructure.Workers;

public class PriceAnalysisWorker : BackgroundService
{
    private readonly IEventConsumer<DataProcessedEvent> _consumer;
    private readonly ProcessPriceDataUseCase _processPriceDataUseCase;
    private readonly ILogger<PriceAnalysisWorker> _logger;

    public PriceAnalysisWorker(
        IEventConsumer<DataProcessedEvent> consumer,
        ProcessPriceDataUseCase processPriceDataUseCase,
        ILogger<PriceAnalysisWorker> logger)
    {
        _consumer = consumer;
        _processPriceDataUseCase = processPriceDataUseCase;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price Analysis Worker started");

        await _consumer.CreateConsumerGroupIfNotExistsAsync(stoppingToken);

        await foreach (var envelope in _consumer.ReadMessagesAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Received data.processed event {EventId}", envelope.Data.EventId);
                await _processPriceDataUseCase.HandleAsync(envelope.Data, stoppingToken);
                await _consumer.AcknowledgeMessageAsync(envelope, stoppingToken);
                _logger.LogInformation("Processed data.processed event {EventId}", envelope.Data.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data.processed event {EventId}", envelope.Data.EventId);
                await _consumer.RejectMessageAsync(envelope, stoppingToken);
            }
        }

        _logger.LogInformation("Price Analysis Worker stopped");
    }
}