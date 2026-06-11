namespace Simcag.PriceAnalysisService.Domain.ValueObjects;

public sealed class MarketPriceSampleLine
{
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public decimal? PriceBrl { get; init; }
    public string? Provider { get; init; }
}
