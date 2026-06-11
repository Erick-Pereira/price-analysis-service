namespace Simcag.PriceAnalysisService.Domain.ValueObjects;

public sealed class MarketPriceEvidenceLine
{
    public string Scope { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Detail { get; init; }
}
