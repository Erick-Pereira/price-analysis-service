using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Application.Benchmarking;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Enums;
using Simcag.PriceAnalysisService.Domain.Events;
using Simcag.PriceAnalysisService.Domain.ValueObjects;

namespace Simcag.PriceAnalysisService.Application.Services;

public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly IPriceAnalysisRepository _analysisRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly ILogger<PriceAnalysisService> _logger;
    private readonly IMarketDataPriceClient _marketDataPriceClient;
    private readonly IMarketDataCacheService _marketDataCache;

    public PriceAnalysisService(
        IPriceAnalysisRepository analysisRepository,
        IPriceRepository priceRepository,
        ILogger<PriceAnalysisService> logger,
        IMarketDataPriceClient marketDataPriceClient,
        IMarketDataCacheService marketDataCache)
    {
        _analysisRepository = analysisRepository;
        _priceRepository = priceRepository;
        _logger = logger;
        _marketDataPriceClient = marketDataPriceClient;
        _marketDataCache = marketDataCache;
    }

    public async Task<PriceAnalysisResult> AnalyzePriceAsync(
        DataProcessedEvent data,
        decimal? historicalAveragePrior,
        CancellationToken ct)
    {
        using var _ = _logger.BeginScope("{ProductId} {EventId}", data.ProductId, data.EventId);
        _logger.LogInformation("Starting price analysis for product {ProductId}", data.ProductId);

        var productName = string.IsNullOrWhiteSpace(data.ProductName) ? data.ProductId : data.ProductName;
        var cacheKey = BuildMarketDataCacheKey(data.Region, data.ProductId, productName, data.Price);
        var (marketPrice, marketLookup) = await GetMarketPriceWithCacheAsync(cacheKey, productName, data.Price, ct);

        var history = await _priceRepository.GetPriceHistoryAsync(data.ProductId, ct) ?? new List<PriceHistory>();
        var pricePoints = history
            .OrderBy(h => h.Timestamp)
            .Select(h => h.Price)
            .ToList();

        var historicalForMetrics = historicalAveragePrior
            ?? ComputeHistoricalAverageExcludingEvent(history, data.EventId)
            ?? (pricePoints.Count > 0 ? pricePoints.Average() : (decimal?)null);

        var deviationVsMarket = marketPrice is > 0m
            ? CalculateDeviationPercent(data.Price, marketPrice.Value)
            : (decimal?)null;
        var deviationVsHistory = historicalForMetrics is > 0m
            ? CalculateDeviationPercent(data.Price, historicalForMetrics.Value)
            : (decimal?)null;

        var (finalSeverity, reportedDeviation) = ResolveDeviation(
            deviationVsMarket,
            deviationVsHistory,
            out var primaryNote);
        var notes = string.IsNullOrEmpty(primaryNote)
            ? BuildNotes(reportedDeviation, finalSeverity, marketPrice, historicalForMetrics)
            : primaryNote;
        var isAnomalous = PriceDeviationPolicy.IsAnomalous(finalSeverity);

        var analysis = PriceAnalysis.Create(
            data.ProductId,
            data.Price,
            marketPrice,
            historicalForMetrics,
            reportedDeviation,
            finalSeverity,
            isAnomalous: null,
            analysisNotes: notes);

        await _analysisRepository.AddAsync(analysis, ct);

        if (!pricePoints.Contains(data.Price))
            pricePoints = [.. pricePoints, data.Price];
        if (pricePoints.Count == 0)
            pricePoints.Add(data.Price);

        var stdDev = ComputePriceStdDev(pricePoints);
        var sorted = pricePoints.OrderBy(p => p).ToList();
        var median = sorted.Count % 2 == 0
            ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
            : sorted[sorted.Count / 2];

        var referenceCenter = marketPrice ?? historicalForMetrics ?? data.Price;
        var safe = CreateSafeZoneAroundPrice(referenceCenter, PriceDeviationPolicy.SafeZoneBandPercent);

        return new PriceAnalysisResult
        {
            ProductId = data.ProductId,
            MarketAverage = marketPrice ?? 0m,
            HistoricalAverage = historicalForMetrics ?? 0m,
            AveragePrice = referenceCenter,
            MedianPrice = median,
            StandardDeviation = stdDev,
            DeviationPercentage = reportedDeviation,
            Severity = finalSeverity,
            SafeZone = safe,
            AnalysisDate = DateTime.UtcNow,
            Anomalies = new List<PriceAnomaly>(),
            MarketSource = marketLookup?.Source,
            MarketBenchmarkKind = marketLookup?.BenchmarkKind,
            MarketBenchmarkStatus = marketLookup?.BenchmarkStatus,
            MarketConfidence = marketLookup?.Confidence,
            MarketSampleCount = marketLookup?.SampleCount,
            MarketRelativeSpread = marketLookup?.RelativeSpread,
            MarketSearchQuery = marketLookup?.SearchQueryUsed,
            MarketDocumentAnchorPrice = marketLookup is not null && IsDocumentAnchorOnly(marketLookup) && marketLookup.Price > 0
                ? marketLookup.Price
                : null,
            MarketEvidence = MapEvidence(marketLookup),
            MarketReferenceLinks = MapReferenceLinks(marketLookup),
            MarketSamples = MapMarketSamples(marketLookup),
        };
    }

    private static IReadOnlyList<MarketPriceSampleLine>? MapMarketSamples(MarketPriceLookupResult? lookup)
    {
        if (lookup?.MarketSamples is not { Count: > 0 } items)
            return null;

        return items
            .Select(s => new MarketPriceSampleLine
            {
                Label = s.Label,
                Url = s.Url,
                PriceBrl = s.PriceBrl,
                Provider = s.Provider,
            })
            .ToList();
    }

    private static IReadOnlyList<MarketPriceReferenceLinkLine>? MapReferenceLinks(MarketPriceLookupResult? lookup)
    {
        if (lookup?.ReferenceLinks is not { Count: > 0 } items)
            return null;

        return items
            .Select(l => new MarketPriceReferenceLinkLine { Label = l.Label, Url = l.Url })
            .ToList();
    }

    private static IReadOnlyList<MarketPriceEvidenceLine>? MapEvidence(MarketPriceLookupResult? lookup)
    {
        if (lookup?.Evidence is not { Count: > 0 } items)
            return null;

        return items
            .Select(e => new MarketPriceEvidenceLine
            {
                Scope = e.Scope,
                Phase = e.Phase,
                Message = e.Message,
                Detail = e.Detail,
            })
            .ToList();
    }

    private static (DeviationSeverity sev, decimal? dev) ResolveDeviation(
        decimal? deviationVsMarket,
        decimal? deviationVsHistory,
        out string? detailWhenNoCandidate)
    {
        var candidates = new List<(decimal? d, DeviationSeverity s)>();
        if (deviationVsMarket.HasValue)
            candidates.Add((deviationVsMarket, PriceDeviationPolicy.Classify(deviationVsMarket)));
        if (deviationVsHistory.HasValue)
            candidates.Add((deviationVsHistory, PriceDeviationPolicy.Classify(deviationVsHistory)));

        if (candidates.Count == 0)
        {
            detailWhenNoCandidate = "No reference (market and internal history) — deviation not computed";
            return (DeviationSeverity.Normal, null);
        }

        // Prefer worst severity, then higher absolute delta (audit: biggest concern wins).
        var best = candidates
            .OrderByDescending(t => (int)t.s)
            .ThenByDescending(t => Math.Abs(t.d!.Value))
            .First();

        detailWhenNoCandidate = null;
        return (best.s, best.d);
    }

    private static string BuildNotes(
        decimal? reportedDeviation,
        DeviationSeverity severity,
        decimal? market,
        decimal? historical)
    {
        if (!reportedDeviation.HasValue)
            return "No reference (market and internal history) — deviation not computed";
        var s = PriceDeviationPolicy.ToAuditString(severity);
        if (PriceDeviationPolicy.IsAnomalous(severity))
            return $"{s} deviation: {reportedDeviation:F2}% (ref market {FormatRef(market)}, ref history {FormatRef(historical)})";
        return $"Normal ({s}): {reportedDeviation:F2}% vs primary reference (market {FormatRef(market)}, history {FormatRef(historical)})";
    }

    private static string FormatRef(decimal? d) => d is null ? "n/a" : $"{d:F2}";

    public async Task<PriceAnalysisResult> RecalculatePriceStatsAsync(string productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recalculating price stats for product {ProductId}", productId);

        var priceHistory = await _priceRepository.GetPriceHistoryAsync(productId, cancellationToken);
        if (priceHistory is null || !priceHistory.Any())
        {
            return new PriceAnalysisResult
            {
                ProductId = productId,
                SafeZone = CreateSafeZoneAroundPrice(0.01m, PriceDeviationPolicy.SafeZoneBandPercent),
                Anomalies = new List<PriceAnomaly>()
            };
        }

        var prices = priceHistory.Select(p => p.Price).ToList();
        var avgPrice = prices.Average();
        var sortedPrices = prices.OrderBy(p => p).ToList();
        var medianPrice = sortedPrices.Count % 2 == 0
            ? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2
            : sortedPrices[sortedPrices.Count / 2];

        var stdDev = (decimal)Math.Sqrt((double)prices.Select(p => (p - avgPrice) * (p - avgPrice)).Average());

        return new PriceAnalysisResult
        {
            ProductId = productId,
            MarketAverage = 0m,
            HistoricalAverage = avgPrice,
            AveragePrice = avgPrice,
            MedianPrice = medianPrice,
            StandardDeviation = stdDev,
            AnalysisDate = DateTime.UtcNow,
            SafeZone = CreateSafeZoneAroundPrice(avgPrice, PriceDeviationPolicy.SafeZoneBandPercent)
        };
    }

    public async Task<PriceAnalysisResult> GetLatestProductAnalysisAsync(string productId, CancellationToken cancellationToken)
    {
        var latest = await _analysisRepository.GetLatestByProductIdAsync(productId, cancellationToken);
        if (latest is null)
            throw new InvalidOperationException($"No analysis found for product '{productId}'");
        return MapStoredAnalysisToApiResult(latest);
    }

    public async Task<IEnumerable<PriceAnomaly>> GetAnomaliesAsync(CancellationToken cancellationToken)
    {
        var rows = await _analysisRepository.GetAnomaliesAsync(1, 500, cancellationToken);
        return rows.Select(MapStoredAnalysisToAnomaly).ToList();
    }

    public async Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisAsync(CancellationToken cancellationToken)
    {
        var rows = await _analysisRepository.GetAllAsync(cancellationToken);
        return rows.Select(MapStoredAnalysisToApiResult).ToList();
    }

    /// <summary>Inclui <paramref name="productId"/> e bucket do declarado para evitar colisão de cache.</summary>
    private static string BuildMarketDataCacheKey(string region, string productId, string productName, decimal declaredPrice)
    {
        var bucket = MarketDeclaredPlausibility.DeclaredPriceBucket(declaredPrice);
        var core = $"{productId.Trim()}|{productName.Trim()}|{bucket}";
        return string.IsNullOrWhiteSpace(region) ? core : $"{region.Trim()}|{core}";
    }

    private static decimal? ComputeHistoricalAverageExcludingEvent(IReadOnlyList<PriceHistory> history, Guid eventId)
    {
        if (eventId == Guid.Empty)
            return null;
        var others = history.Where(h => h.EventId != eventId).ToList();
        if (others.Count == 0)
            return null;
        return others.Average(h => h.Price);
    }

    private async Task<(decimal? price, MarketPriceLookupResult? lookup)> GetMarketPriceWithCacheAsync(
        string cacheKey,
        string productName,
        decimal lineDeclaredPrice,
        CancellationToken ct)
    {
        var cached = await _marketDataCache.GetPriceAsync(cacheKey);
        if (cached.HasValue)
        {
            if (lineDeclaredPrice <= 0.01m
                || MarketDeclaredPlausibility.IsPlausible(cached.Value, lineDeclaredPrice))
            {
                return (cached.Value, new MarketPriceLookupResult
                {
                    Price = cached.Value,
                    Source = "RedisCache:1h",
                    BenchmarkKind = "ExternalMarketEstimate",
                    BenchmarkStatus = "cached",
                    Confidence = "medium",
                });
            }

            _logger.LogInformation(
                "Cache price-analysis ignorado para {ProductName}: {CachedPrice} incompatível com declarado {Declared}",
                productName,
                cached.Value,
                lineDeclaredPrice);
        }

        var lookup = await GetMarketPriceFromServiceAsync(productName, lineDeclaredPrice, ct);
        if (lookup is null)
            return (null, null);

        if (IsDocumentAnchorOnly(lookup))
            return (null, lookup);

        await _marketDataCache.SetPriceAsync(cacheKey, lookup.Price);
        return (lookup.Price, lookup);
    }

    private async Task<MarketPriceLookupResult?> GetMarketPriceFromServiceAsync(
        string productName,
        decimal lineDeclaredPrice,
        CancellationToken ct)
    {
        var lookup = await _marketDataPriceClient.LookupPriceAsync(productName, lineDeclaredPrice, ct);
        if (lookup is null)
        {
            _logger.LogWarning("Market data RPC returned no price for {ProductName}", productName);
            return null;
        }

        if (IsDocumentAnchorOnly(lookup))
        {
            _logger.LogInformation(
                "Market data retornou apenas âncora documental para {ProductName}; benchmark externo indisponível (web scrape sem amostras).",
                productName);
            return lookup;
        }

        _logger.LogInformation(
            "Retrieved market price {Price} for {ProductName} (source={Source})",
            lookup.Price,
            productName,
            lookup.Source);
        return lookup;
    }

    private static decimal ComputePriceStdDev(IReadOnlyList<decimal> prices)
    {
        if (prices.Count < 2) return 0m;
        var mean = prices.Average();
        return (decimal)Math.Sqrt((double)prices.Sum(p => (p - mean) * (p - mean)) / prices.Count);
    }

    private static decimal CalculateDeviationPercent(decimal originalPrice, decimal marketPrice) =>
        marketPrice == 0m ? 0m : (originalPrice - marketPrice) / marketPrice * 100m;

    private static PriceRange CreateSafeZoneAroundPrice(decimal center, decimal bandPercent)
    {
        if (center <= 0)
        {
            return new PriceRange(0.01m, 0.02m);
        }
        var low = center * (1 - bandPercent);
        var high = center * (1 + bandPercent);
        if (low >= high)
            high = center + 0.01m;
        return new PriceRange(low, high);
    }

    private static PriceAnalysisResult MapStoredAnalysisToApiResult(PriceAnalysis analysis)
    {
        var refPrice = analysis.MarketPrice ?? analysis.OriginalPrice;
        return new PriceAnalysisResult
        {
            ProductId = analysis.ProductId,
            MarketAverage = analysis.MarketPrice ?? 0m,
            HistoricalAverage = analysis.HistoricalAverage ?? 0m,
            AveragePrice = refPrice,
            MedianPrice = refPrice,
            StandardDeviation = 0,
            DeviationPercentage = analysis.DeviationPercentage,
            Severity = analysis.Severity,
            SafeZone = CreateSafeZoneAroundPrice(refPrice, PriceDeviationPolicy.SafeZoneBandPercent),
            AnalysisDate = analysis.AnalysisDate,
            Anomalies = PriceDeviationPolicy.IsAnomalous(analysis.Severity)
                ? new List<PriceAnomaly> { MapStoredAnalysisToAnomaly(analysis) }
                : new List<PriceAnomaly>()
        };
    }

    private static PriceAnomaly MapStoredAnalysisToAnomaly(PriceAnalysis analysis)
    {
        var avg = analysis.MarketPrice ?? analysis.OriginalPrice;
        var allowed = CreateSafeZoneAroundPrice(avg, PriceDeviationPolicy.SafeZoneBandPercent);
        var deviation = analysis.DeviationPercentage ?? 0m;
        var type = deviation switch
        {
            > 0 => PriceAnomalyType.HighAnomaly,
            < 0 => PriceAnomalyType.LowAnomaly,
            _ => PriceAnomalyType.None
        };
        return new PriceAnomaly
        {
            Id = analysis.Id.ToString(),
            ProductId = analysis.ProductId,
            CurrentPrice = analysis.OriginalPrice,
            AveragePrice = avg,
            AllowedRange = allowed,
            AnomalyType = type,
            Timestamp = analysis.AnalysisDate,
            Message = analysis.AnalysisNotes
        };
    }

    private static bool IsDocumentAnchorOnly(MarketPriceLookupResult data) =>
        string.Equals(data.BenchmarkKind, "DocumentAnchorPrice", StringComparison.OrdinalIgnoreCase)
        || string.Equals(data.Source, "DocumentDeclaredReference", StringComparison.OrdinalIgnoreCase)
        || string.Equals(data.BenchmarkStatus, "document_anchor", StringComparison.OrdinalIgnoreCase);
}
