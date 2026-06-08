namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IMarketDataPriceClient
{
    Task<MarketPriceLookupResult?> LookupPriceAsync(
        string productName,
        decimal declaredReferenceBrl,
        CancellationToken cancellationToken);
}

public sealed class MarketPriceLookupResult
{
    public decimal Price { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? BenchmarkKind { get; init; }
    public string? BenchmarkStatus { get; init; }
}
