using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.Shared.Events;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Application.Services;

public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly IPriceAnalysisRepository _analysisRepository;
    private readonly IEventPublisher<PriceAnalyzedEvent> _eventPublisher;
    private readonly ILogger<PriceAnalysisService> _logger;
    private readonly HttpClient _httpClient;

    public PriceAnalysisService(
        IPriceAnalysisRepository analysisRepository,
        IEventPublisher<PriceAnalyzedEvent> eventPublisher,
        ILogger<PriceAnalysisService> logger,
        HttpClient httpClient)
    {
        _analysisRepository = analysisRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task AnalyzePriceAsync(DataProcessedEvent dataProcessedEvent, CancellationToken ct)
    {
        using var scope = _logger.BeginScope("{ProductId} {EventId}", dataProcessedEvent.ProductId, dataProcessedEvent.EventId);

        _logger.LogInformation("Starting price analysis for product {ProductId}", dataProcessedEvent.ProductId);

        try
        {
            // Get market price from Market Data Service
            var marketPrice = await GetMarketPriceAsync(dataProcessedEvent.ProductName, ct);

            // Calculate deviation and determine anomaly
            decimal? deviationPercentage = null;
            var isAnomalous = false;
            string? analysisNotes = null;

            if (marketPrice.HasValue)
            {
                deviationPercentage = CalculateDeviationPercentage(dataProcessedEvent.Price, marketPrice.Value);
                isAnomalous = Math.Abs(deviationPercentage.Value) > 10; // >10% deviation is anomalous

                analysisNotes = isAnomalous
                    ? $"Anomalous price deviation: {deviationPercentage:F2}% (Market: {marketPrice:C}, Product: {dataProcessedEvent.Price:C})"
                    : $"Normal price within range: {deviationPercentage:F2}% deviation";

                _logger.LogInformation("Price analysis completed: Deviation {Deviation:F2}%, Anomalous: {IsAnomalous}",
                    deviationPercentage, isAnomalous);
            }
            else
            {
                analysisNotes = "Market price not available - cannot determine deviation";
                _logger.LogWarning("Market price not found for product {ProductName}", dataProcessedEvent.ProductName);
            }

            // Create analysis record
            var analysis = PriceAnalysis.Create(
                productId: dataProcessedEvent.ProductId,
                originalPrice: dataProcessedEvent.Price,
                marketPrice: marketPrice,
                deviationPercentage: deviationPercentage,
                isAnomalous: isAnomalous,
                analysisNotes: analysisNotes);

            await _analysisRepository.AddAsync(analysis, ct);

            // Publish price analyzed event
            var priceAnalyzedEvent = new PriceAnalyzedEvent
            {
                ProductId = dataProcessedEvent.ProductId,
                AveragePrice = marketPrice ?? 0,
                LastPrice = dataProcessedEvent.Price,
                PriceVariation = deviationPercentage ?? 0,
                Trend = DetermineTrend(marketPrice, dataProcessedEvent.Price)
            };

            await _eventPublisher.PublishAsync(priceAnalyzedEvent, ct);

            _logger.LogInformation("Price analysis completed and event published for product {ProductId}", dataProcessedEvent.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze price for product {ProductId}", dataProcessedEvent.ProductId);
            throw;
        }
    }

    private async Task<decimal?> GetMarketPriceAsync(string productName, CancellationToken ct)
    {
        try
        {
            // Call Market Data Service
            var response = await _httpClient.GetAsync($"/api/marketdata/price?productName={Uri.EscapeDataString(productName)}", ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MarketDataResponse>(ct);
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("Retrieved market price {Price:C} for product {ProductName}",
                        result.Data.Price, productName);
                    return result.Data.Price;
                }
            }

            _logger.LogWarning("Failed to retrieve market price for {ProductName}. Status: {StatusCode}",
                productName, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Market Data Service for product {ProductName}", productName);
            return null;
        }
    }

    private decimal CalculateDeviationPercentage(decimal originalPrice, decimal marketPrice)
    {
        if (marketPrice == 0)
            return 0;

        return ((originalPrice - marketPrice) / marketPrice) * 100;
    }

    private string DetermineTrend(decimal? marketPrice, decimal currentPrice)
    {
        if (!marketPrice.HasValue)
            return "UNKNOWN";

        var variation = ((currentPrice - marketPrice.Value) / marketPrice.Value) * 100;

        if (variation >= 5) return "UP";
        if (variation <= -5) return "DOWN";
        return "STABLE";
    }
}

// Response DTO for Market Data Service
public class MarketDataResponse
{
    public bool Success { get; set; }
    public MarketData? Data { get; set; }
    public string? Message { get; set; }
    public string[]? Errors { get; set; }
}

public class MarketData
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CollectedDate { get; set; }
}