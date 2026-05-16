using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Messaging.Telemetry;
using Simcag.PriceAnalysisService.Application.Mapping;
using Simcag.PriceAnalysisService.Application.UseCases;
using Simcag.PriceAnalysisService.Domain.Events;

namespace Simcag.PriceAnalysisService.Application.Workers;

public class DataProcessedEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataProcessedEventConsumer> _logger;
    private readonly IEventConsumer<PriceDataProcessedEvent> _eventConsumer;

    public DataProcessedEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<DataProcessedEventConsumer> logger,
        IEventConsumer<PriceDataProcessedEvent> eventConsumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventConsumer = eventConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting PriceDataProcessed event consumer (price analysis pipeline)");

        await foreach (var messageEnvelope in _eventConsumer.ReadMessagesAsync(stoppingToken))
        {
            using (MessagingConsumeTelemetry.BeginConsume(messageEnvelope, out _))
            {
                using var scope = _scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessPriceDataUseCase>();

                try
                {
                    var incoming = messageEnvelope.Data;
                    var audit = ProcessedEventDataMapper.MapAudit(incoming);
                    var mapped = new DataProcessedEvent(
                        incoming.EventId,
                        incoming.ProductId,
                        incoming.Price,
                        incoming.Timestamp,
                        incoming.Source,
                        incoming.Market)
                    {
                        ProductName = incoming.ProductName,
                        ExpenseId = audit.ExpenseId,
                        TenantId = audit.TenantId ?? string.Empty,
                        Category = audit.Category ?? string.Empty,
                        Region = audit.Region ?? string.Empty,
                        SupplierId = audit.SupplierId ?? string.Empty
                    };

                    await useCase.HandleAsync(mapped, stoppingToken);
                    await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
                    _logger.LogInformation("Processed price-data event {EventId} for product {ProductId}",
                        mapped.EventId, mapped.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process price-data event {EventId} for {ProductId}",
                        messageEnvelope.Data.EventId, messageEnvelope.Data.ProductId);
                    await _eventConsumer.RejectMessageAsync(messageEnvelope, stoppingToken);
                }
            }
        }

        _logger.LogInformation("PriceDataProcessed event consumer stopped");
    }
}
