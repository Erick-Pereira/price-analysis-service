using System.ComponentModel.DataAnnotations;
using Simcag.PriceAnalysisService.Domain.Enums;
using Simcag.PriceAnalysisService.Domain.ValueObjects;
using static Simcag.PriceAnalysisService.Domain.PriceDeviationPolicy;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceAnalysisResult
{
    [Key]
    public string ProductId { get; set; } = string.Empty;

    public decimal MarketAverage { get; set; }
    public decimal HistoricalAverage { get; set; }
    [Required] public decimal AveragePrice { get; set; }
    [Required] public decimal MedianPrice { get; set; }
    [Required] public decimal StandardDeviation { get; set; }
    public decimal? DeviationPercentage { get; set; }
    public DeviationSeverity Severity { get; set; } = DeviationSeverity.Normal;
    [Required] public PriceRange SafeZone { get; set; } = new(0, 0.01m);
    [Required]     public DateTime AnalysisDate { get; set; }
    public List<PriceAnomaly> Anomalies { get; set; } = new();
    public bool HasAnomalies => IsAnomalous(Severity) || Anomalies.Any();

    public string? MarketSource { get; set; }
    public string? MarketBenchmarkKind { get; set; }
    public string? MarketBenchmarkStatus { get; set; }
    public string? MarketConfidence { get; set; }
    public int? MarketSampleCount { get; set; }
    public decimal? MarketRelativeSpread { get; set; }
    public string? MarketSearchQuery { get; set; }
    public decimal? MarketDocumentAnchorPrice { get; set; }
    public IReadOnlyList<MarketPriceEvidenceLine>? MarketEvidence { get; set; }
    public IReadOnlyList<MarketPriceReferenceLinkLine>? MarketReferenceLinks { get; set; }
    public IReadOnlyList<MarketPriceSampleLine>? MarketSamples { get; set; }
}