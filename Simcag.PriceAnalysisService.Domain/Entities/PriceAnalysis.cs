using Simcag.PriceAnalysisService.Domain.Enums;
using Simcag.Shared.Common;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceAnalysis : BaseEntity
{
    public string ProductId { get; private set; } = string.Empty;
    public decimal OriginalPrice { get; private set; }
    public decimal? MarketPrice { get; private set; }
    public decimal? HistoricalAverage { get; private set; }
    public decimal? DeviationPercentage { get; private set; }
    public DeviationSeverity Severity { get; private set; }
    public DateTime AnalysisDate { get; private set; }
    public bool IsAnomalous { get; private set; }
    public string? AnalysisNotes { get; private set; }

    private PriceAnalysis()
    { } // EF Core

    public static PriceAnalysis Create(
        string productId,
        decimal originalPrice,
        decimal? marketPrice = null,
        decimal? historicalAverage = null,
        decimal? deviationPercentage = null,
        DeviationSeverity severity = DeviationSeverity.Normal,
        bool? isAnomalous = null,
        string? analysisNotes = null)
    {
        var anom = isAnomalous ?? PriceDeviationPolicy.IsAnomalous(severity);
        return new PriceAnalysis
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OriginalPrice = originalPrice,
            MarketPrice = marketPrice,
            HistoricalAverage = historicalAverage,
            DeviationPercentage = deviationPercentage,
            Severity = severity,
            AnalysisDate = DateTime.UtcNow,
            IsAnomalous = anom,
            AnalysisNotes = analysisNotes
        };
    }

    public void UpdateAnalysis(
        decimal? marketPrice,
        decimal? historicalAverage,
        decimal? deviationPercentage,
        DeviationSeverity severity,
        bool? isAnomalous,
        string? notes = null)
    {
        MarketPrice = marketPrice;
        HistoricalAverage = historicalAverage;
        DeviationPercentage = deviationPercentage;
        Severity = severity;
        IsAnomalous = isAnomalous ?? PriceDeviationPolicy.IsAnomalous(severity);
        AnalysisNotes = notes;
        AnalysisDate = DateTime.UtcNow;
    }
}