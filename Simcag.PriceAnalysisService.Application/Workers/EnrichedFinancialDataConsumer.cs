using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Application.Mapping;
using Simcag.PriceAnalysisService.Application.UseCases;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Application.Workers;

/// <summary>
/// Consome <see cref="EnrichedFinancialDataEvent"/> (publicado pelo AI Service) e executa análise de preço
/// por linha com valor &gt; 0 — inclui chamadas HTTP ao Market Data Service.
/// </summary>
public sealed class EnrichedFinancialDataConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrichedFinancialDataConsumer> _logger;
    private readonly IEventConsumer<EnrichedFinancialDataEvent> _consumer;

    public EnrichedFinancialDataConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<EnrichedFinancialDataConsumer> logger,
        IEventConsumer<EnrichedFinancialDataEvent> consumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnrichedFinancialDataConsumer iniciado (fila enriched-financial-data-events → análise / market-data).");

        await foreach (var envelope in _consumer.ReadMessagesAsync(stoppingToken))
        {
            var enriched = envelope.Data;
            try
            {
                var mapped = EnrichedFinancialDataMapper.ToDataProcessedEvents(enriched).ToList();
                if (mapped.Count == 0)
                {
                    _logger.LogInformation(
                        "EnrichedFinancialDataEvent {EventId} sem linhas com valor > 0 (documento {DocumentId}); ack.",
                        enriched.EventId,
                        enriched.DocumentId);
                    await _consumer.AcknowledgeMessageAsync(envelope, stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessPriceDataUseCase>();

                foreach (var row in mapped)
                    await useCase.HandleAsync(row, stoppingToken);

                await _consumer.AcknowledgeMessageAsync(envelope, stoppingToken);
                _logger.LogInformation(
                    "EnrichedFinancialDataEvent {EventId}: analisadas {Count} linha(s) (documento {DocumentId}).",
                    enriched.EventId,
                    mapped.Count,
                    enriched.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao processar EnrichedFinancialDataEvent {EventId} documento {DocumentId}",
                    enriched.EventId,
                    enriched.DocumentId);
                await _consumer.RejectMessageAsync(envelope, stoppingToken);
            }
        }
    }
}
