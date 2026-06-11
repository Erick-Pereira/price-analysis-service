using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Events;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Application.UseCases;

public sealed class ProcessPriceDataUseCase
{
    private readonly IPriceRepository _priceRepository;
    private readonly IPriceAnalysisService _priceAnalysisService;
    private readonly DetectPriceVariationUseCase _variationUseCase;
    private readonly IEventPublisher<global::Simcag.Shared.Events.PriceAnalyzedEvent> _priceAnalyzedPublisher;
    private readonly IEventPublisher<Simcag.PriceAnalysisService.Domain.Events.PriceUpdatedEvent> _priceUpdatedPublisher;

    public ProcessPriceDataUseCase(
        IPriceRepository priceRepository,
        IPriceAnalysisService priceAnalysisService,
        DetectPriceVariationUseCase variationUseCase,
        IEventPublisher<global::Simcag.Shared.Events.PriceAnalyzedEvent> priceAnalyzedPublisher,
        IEventPublisher<Simcag.PriceAnalysisService.Domain.Events.PriceUpdatedEvent> priceUpdatedPublisher)
    {
        _priceRepository = priceRepository;
        _priceAnalysisService = priceAnalysisService;
        _variationUseCase = variationUseCase;
        _priceAnalyzedPublisher = priceAnalyzedPublisher;
        _priceUpdatedPublisher = priceUpdatedPublisher;
    }

    public async Task HandleAsync(DataProcessedEvent input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (await _priceRepository.ExistsByEventIdAsync(input.EventId, cancellationToken))
            return;

        var prior = await _priceRepository.GetPriceHistoryAsync(input.ProductId, cancellationToken);
        var historicalAveragePrior = prior is { Count: > 0 }
            ? prior.Average(h => h.Price)
            : (decimal?)null;

        var priceHistory = new PriceHistory(
            input.EventId,
            input.ProductId,
            input.Price,
            input.Timestamp,
            input.Source,
            input.Market);

        await _priceRepository.AddPriceHistoryAsync(priceHistory, cancellationToken);
        await _priceRepository.MarkEventAsProcessedAsync(input.EventId, cancellationToken);

        var analysisResult = await _priceAnalysisService.AnalyzePriceAsync(
            input,
            historicalAveragePrior,
            cancellationToken);

        var variation = _variationUseCase.Execute(analysisResult, input.Price);
        if (variation is not null)
        {
            await _priceRepository.SaveAnomalyAsync(variation, cancellationToken);
        }

        var trend = PriceTrend.Calculate(input.Price, analysisResult.MarketAverage);

        var historyList = await _priceRepository.GetPriceHistoryAsync(input.ProductId, cancellationToken) ?? new List<PriceHistory>();
        var points = historyList
            .Select(h => h.Price)
            .ToList();
        if (points.Count == 0)
            points.Add(input.Price);

        var productName = string.IsNullOrWhiteSpace(input.ProductName) ? input.ProductId : input.ProductName;
        var category = !string.IsNullOrWhiteSpace(input.Category)
            ? input.Category
            : (string.IsNullOrWhiteSpace(input.Market) ? input.Source : input.Market);

        await _priceAnalyzedPublisher.PublishAsync(new global::Simcag.Shared.Events.PriceAnalyzedEvent
        {
            RawDocumentId = input.RawDocumentId,
            ExpenseId = input.ExpenseId,
            TenantId = input.TenantId,
            ProductId = analysisResult.ProductId,
            ProductName = productName,
            Category = category,
            Region = input.Region,
            SupplierId = input.SupplierId,
            LastPrice = input.Price,
            Quantity = input.Quantity,
            LineTotal = input.LineTotal,
            MarketAverage = analysisResult.MarketAverage,
            HistoricalAverage = analysisResult.HistoricalAverage,
            DeviationPercentage = analysisResult.DeviationPercentage ?? 0m,
            PriceVariation = analysisResult.DeviationPercentage ?? 0m,
            Severity = PriceDeviationPolicy.ToAuditString(analysisResult.Severity),
            AveragePrice = analysisResult.MarketAverage,
            MedianPrice = analysisResult.MedianPrice,
            StandardDeviation = analysisResult.StandardDeviation,
            SafeZoneMin = analysisResult.SafeZone.Min,
            SafeZoneMax = analysisResult.SafeZone.Max,
            Trend = trend,
            AnalysisDate = analysisResult.AnalysisDate,
            HasAnomalies = analysisResult.HasAnomalies,
            MarketSource = analysisResult.MarketSource,
            MarketBenchmarkKind = analysisResult.MarketBenchmarkKind,
            MarketBenchmarkStatus = analysisResult.MarketBenchmarkStatus,
            MarketConfidence = analysisResult.MarketConfidence,
            MarketSampleCount = analysisResult.MarketSampleCount,
            MarketRelativeSpread = analysisResult.MarketRelativeSpread,
            MarketSearchQuery = analysisResult.MarketSearchQuery,
            MarketDocumentAnchorPrice = analysisResult.MarketDocumentAnchorPrice,
            MarketEvidence = analysisResult.MarketEvidence?
                .Select(e => new global::Simcag.Shared.Contracts.MarketPriceEvidence
                {
                    Scope = e.Scope,
                    Phase = e.Phase,
                    Message = e.Message,
                    Detail = e.Detail,
                })
                .ToList() ?? new List<global::Simcag.Shared.Contracts.MarketPriceEvidence>(),
            MarketReferenceLinks = analysisResult.MarketReferenceLinks?
                .Select(l => new global::Simcag.Shared.Contracts.MarketPriceReferenceLink
                {
                    Label = l.Label,
                    Url = l.Url,
                })
                .ToList() ?? new List<global::Simcag.Shared.Contracts.MarketPriceReferenceLink>(),
            MarketSamples = analysisResult.MarketSamples?
                .Select(s => new global::Simcag.Shared.Contracts.MarketPriceSample
                {
                    Label = s.Label,
                    Url = s.Url,
                    PriceBrl = s.PriceBrl,
                    Provider = s.Provider,
                })
                .ToList() ?? new List<global::Simcag.Shared.Contracts.MarketPriceSample>(),
            NotifyUserId = input.NotifyUserId is { } uid && uid != Guid.Empty ? uid : null,
            PriceHistory = points
        }, cancellationToken);

        await _priceUpdatedPublisher.PublishAsync(new Simcag.PriceAnalysisService.Domain.Events.PriceUpdatedEvent
        {
            ProductId = analysisResult.ProductId,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}

internal static class PriceTrend
{
    public static string Calculate(decimal lastPrice, decimal marketAverage)
    {
        if (marketAverage <= 0m) return "UNKNOWN";
        var variation = (lastPrice - marketAverage) / marketAverage * 100m;
        if (variation >= PriceDeviationPolicy.TrendUpPercent) return "UP";
        if (variation <= PriceDeviationPolicy.TrendDownPercent) return "DOWN";
        return "STABLE";
    }
}
