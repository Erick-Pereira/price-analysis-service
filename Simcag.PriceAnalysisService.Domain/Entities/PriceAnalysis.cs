using Simcag.Shared.Common;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceAnalysis : BaseEntity
{
    public string ProductId { get; private set; } = string.Empty;
    public decimal OriginalPrice { get; private set; }
    public decimal? MarketPrice { get; private set; }
    public decimal? DeviationPercentage { get; private set; }
    public DateTime AnalysisDate { get; private set; }
    public bool IsAnomalous { get; private set; }
    public string? AnalysisNotes { get; private set; }

    private PriceAnalysis()
    { } // EF Core

    public static PriceAnalysis Create(
        string productId,
        decimal originalPrice,
        decimal? marketPrice = null,
        decimal? deviationPercentage = null,
        bool isAnomalous = false,
        string? analysisNotes = null)
    {
        return new PriceAnalysis
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OriginalPrice = originalPrice,
            MarketPrice = marketPrice,
            DeviationPercentage = deviationPercentage,
            AnalysisDate = DateTime.UtcNow,
            IsAnomalous = isAnomalous,
            AnalysisNotes = analysisNotes
        };
    }

    public void UpdateAnalysis(decimal? marketPrice, decimal? deviationPercentage, bool isAnomalous, string? notes = null)
    {
        MarketPrice = marketPrice;
        DeviationPercentage = deviationPercentage;
        IsAnomalous = isAnomalous;
        AnalysisNotes = notes;
        AnalysisDate = DateTime.UtcNow;
    }
}