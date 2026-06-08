using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Infrastructure.MarketData;

/// <summary>Ambiente Testing — sem RabbitMQ.</summary>
public sealed class NullMarketDataPriceClient : IMarketDataPriceClient
{
    public Task<MarketPriceLookupResult?> LookupPriceAsync(
        string productName,
        decimal declaredReferenceBrl,
        CancellationToken cancellationToken) =>
        Task.FromResult<MarketPriceLookupResult?>(null);
}
