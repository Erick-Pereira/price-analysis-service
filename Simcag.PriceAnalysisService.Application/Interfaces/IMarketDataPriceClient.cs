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
    public string? Confidence { get; init; }
    public int? SampleCount { get; init; }
    public decimal? RelativeSpread { get; init; }
    public string? SearchQueryUsed { get; init; }
    public DateTime? CollectedDate { get; init; }
    public IReadOnlyList<string>? BenchmarkRejectionTrail { get; init; }
    public IReadOnlyList<MarketPriceEvidenceItem>? Evidence { get; init; }
    public IReadOnlyList<MarketPriceReferenceLinkItem>? ReferenceLinks { get; init; }
    public IReadOnlyList<MarketPriceSampleItem>? MarketSamples { get; init; }
}

public sealed class MarketPriceSampleItem
{
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public decimal? PriceBrl { get; init; }
    public string? Provider { get; init; }
}

public sealed class MarketPriceReferenceLinkItem
{
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public sealed class MarketPriceEvidenceItem
{
    public string Scope { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Detail { get; init; }
}
