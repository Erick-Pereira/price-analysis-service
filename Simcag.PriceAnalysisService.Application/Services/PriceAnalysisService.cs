using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Application.Services;

public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly IPriceStatisticsService _statisticsService;
    private readonly IPriceOutlierDetectionService _outlierDetectionService;
    private readonly IPriceRepository _priceRepository;
    private readonly IMarketDataCacheService _cacheService;

    public PriceAnalysisService(
        IPriceStatisticsService statisticsService,
        IPriceOutlierDetectionService outlierDetectionService,
        IPriceRepository priceRepository,
        IMarketDataCacheService cacheService)
    {
        _statisticsService = statisticsService;
        _outlierDetectionService = outlierDetectionService;
        _priceRepository = priceRepository;
        _cacheService = cacheService;
    }

    public async Task<PriceAnalysisResult> AnalyzePriceAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
   
        var cachedPrice = await _cacheService.GetPriceAsync(productId);

        decimal currentPrice;

        if (cachedPrice.HasValue)
        {
            currentPrice = cachedPrice.Value;
        }
        else
        {
            var priceHistory = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);

            if (priceHistory.Count < 5)
                throw new InvalidOperationException($"Insufficient data for product {productId}.");

            currentPrice = priceHistory
                .OrderByDescending(p => p.Timestamp)
                .First().Price;

            await _cacheService.SetPriceAsync(productId, currentPrice);
        }

        var history = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);
        var aggregate = ProductPriceAggregate.FromHistory(productId, history);

        var anomalies = await _outlierDetectionService
            .DetectAnomaliesAsync(currentPrice, aggregate, cancellationToken);

        var result = new PriceAnalysisResult
        {
            ProductId = productId,
            AveragePrice = aggregate.AveragePrice,
            MedianPrice = aggregate.MedianPrice,
            StandardDeviation = aggregate.StandardDeviation,
            SafeZone = aggregate.SafeZone,
            AnalysisDate = DateTime.UtcNow,
            Anomalies = anomalies.ToList()
        };

        await _priceRepository.SaveAnalysisResultAsync(result, cancellationToken);

        return result;
    }

    public async Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisAsync(
        CancellationToken cancellationToken = default)
    {
        return await _priceRepository.GetAllAnalysisResultsAsync(cancellationToken);
    }

    public async Task<IEnumerable<PriceAnomaly>> GetAnomaliesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _priceRepository.GetAllAnomaliesAsync(cancellationToken);
    }

    public async Task<PriceAnalysisResult> RecalculatePriceStatsAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var priceHistory = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);

        if (priceHistory.Count < 5)
            throw new InvalidOperationException($"Insufficient data.");

        var aggregate = ProductPriceAggregate.FromHistory(productId, priceHistory);
        var existing = await _priceRepository.GetAnalysisResultAsync(productId, cancellationToken);

        if (existing == null)
            throw new InvalidOperationException("No analysis found.");

        existing.AveragePrice = aggregate.AveragePrice;
        existing.MedianPrice = aggregate.MedianPrice;
        existing.StandardDeviation = aggregate.StandardDeviation;
        existing.SafeZone = aggregate.SafeZone;
        existing.AnalysisDate = DateTime.UtcNow;

        await _priceRepository.SaveAnalysisResultAsync(existing, cancellationToken);

        return existing;
    }
}