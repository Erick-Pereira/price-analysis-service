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

    public PriceAnalysisService(
        IPriceStatisticsService statisticsService,
        IPriceOutlierDetectionService outlierDetectionService,
        IPriceRepository priceRepository)
    {
        _statisticsService = statisticsService;
        _outlierDetectionService = outlierDetectionService;
        _priceRepository = priceRepository;
    }

    public async Task<PriceAnalysisResult> AnalyzePriceAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var priceHistory = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);

        if (priceHistory.Count < 5)
            throw new InvalidOperationException($"Insufficient data for product {productId}. At least 5 price records are required.");

        var aggregate = ProductPriceAggregate.FromHistory(productId, priceHistory);
        var currentPrice = priceHistory.OrderByDescending(p => p.Timestamp).First().Price;
        var anomalies = await _outlierDetectionService.DetectAnomaliesAsync(currentPrice, aggregate, cancellationToken);

        var analysisResult = new PriceAnalysisResult
        {
            ProductId = productId,
            AveragePrice = aggregate.AveragePrice,
            MedianPrice = aggregate.MedianPrice,
            StandardDeviation = aggregate.StandardDeviation,
            SafeZone = aggregate.SafeZone,
            AnalysisDate = DateTime.UtcNow,
            Anomalies = anomalies.ToList()
        };

        await _priceRepository.SaveAnalysisResultAsync(analysisResult, cancellationToken);

        return analysisResult;
    }

    public async Task<PriceAnalysisResult> RecalculatePriceStatsAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var priceHistory = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);

        if (priceHistory.Count < 5)
            throw new InvalidOperationException($"Insufficient data for product {productId}. At least 5 price records are required.");

        var aggregate = ProductPriceAggregate.FromHistory(productId, priceHistory);
        var existingAnalysis = await _priceRepository.GetAnalysisResultAsync(productId, cancellationToken);

        if (existingAnalysis == null)
            throw new InvalidOperationException($"No existing analysis found for product {productId}.");

        existingAnalysis.AveragePrice = aggregate.AveragePrice;
        existingAnalysis.MedianPrice = aggregate.MedianPrice;
        existingAnalysis.StandardDeviation = aggregate.StandardDeviation;
        existingAnalysis.SafeZone = aggregate.SafeZone;
        existingAnalysis.AnalysisDate = DateTime.UtcNow;

        await _priceRepository.SaveAnalysisResultAsync(existingAnalysis, cancellationToken);

        return existingAnalysis;
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
}
