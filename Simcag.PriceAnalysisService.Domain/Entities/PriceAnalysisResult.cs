using Simcag.PriceAnalysisService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceAnalysisResult
{
    [Key]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    public decimal AveragePrice { get; set; }

    [Required]
    public decimal MedianPrice { get; set; }

    [Required]
    public decimal StandardDeviation { get; set; }

    [Required]
    public PriceRange SafeZone { get; set; } = new PriceRange(0, 0);

    [Required]
    public DateTime AnalysisDate { get; set; }

    public List<PriceAnomaly> Anomalies { get; set; } = new();

    public bool HasAnomalies => Anomalies.Any();
}