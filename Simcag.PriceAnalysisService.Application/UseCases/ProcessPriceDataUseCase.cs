using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Application.UseCases;

public sealed class ProcessPriceDataUseCase
{
    private readonly IPriceRepository _priceRepository;
    private readonly IPriceAnalysisService _priceAnalysisService;
    private readonly DetectPriceVariationUseCase _variationUseCase;
    private readonly IEventPublisher<PriceAnalyzedEvent> _priceAnalyzedPublisher;
    private readonly IEventPublisher<PriceUpdatedEvent> _priceUpdatedPublisher;

    public ProcessPriceDataUseCase(
        IPriceRepository priceRepository,
        IPriceAnalysisService priceAnalysisService,
        DetectPriceVariationUseCase variationUseCase,
        IEventPublisher<PriceAnalyzedEvent> priceAnalyzedPublisher,
        IEventPublisher<PriceUpdatedEvent> priceUpdatedPublisher)
    {
        _priceRepository = priceRepository;
        _priceAnalysisService = priceAnalysisService;
        _variationUseCase = variationUseCase;
        _priceAnalyzedPublisher = priceAnalyzedPublisher;
        _priceUpdatedPublisher = priceUpdatedPublisher;
    }

    public async Task HandleAsync(DataProcessedEvent input, CancellationToken cancellationToken)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (await _priceRepository.ExistsByEventIdAsync(input.EventId, cancellationToken))
            return;

        var priceHistory = new PriceHistory(
            input.EventId,
            input.ProductId,
            input.Price,
            input.Timestamp,
            input.Source,
            input.Market);

        await _priceRepository.AddPriceHistoryAsync(priceHistory, cancellationToken);
        await _priceRepository.MarkEventAsProcessedAsync(input.EventId, cancellationToken);

        var analysisResult = await _priceAnalysisService.AnalyzePriceAsync(input.ProductId, cancellationToken);

        var variation = _variationUseCase.Execute(analysisResult, input.Price);
        if (variation != null)
        {
            await _priceRepository.SaveAnomalyAsync(variation, cancellationToken);
        }

        await _priceAnalyzedPublisher.PublishAsync(new PriceAnalyzedEvent
        {
            ProductId = analysisResult.ProductId,
            AveragePrice = analysisResult.AveragePrice,
            MedianPrice = analysisResult.MedianPrice,
            StandardDeviation = analysisResult.StandardDeviation,
            SafeZoneMin = analysisResult.SafeZone.Min,
            SafeZoneMax = analysisResult.SafeZone.Max,
            AnalysisDate = analysisResult.AnalysisDate,
            HasAnomalies = analysisResult.HasAnomalies
        }, cancellationToken);

        await _priceUpdatedPublisher.PublishAsync(new PriceUpdatedEvent
        {
            ProductId = analysisResult.ProductId,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
