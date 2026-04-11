using System.ComponentModel.DataAnnotations;
using Simcag.PriceAnalysisService.Domain.Enums;
using Simcag.PriceAnalysisService.Domain.ValueObjects;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceAnomaly
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    public decimal CurrentPrice { get; set; }

    [Required]
    public decimal AveragePrice { get; set; }

    [Required]
    public PriceRange AllowedRange { get; set; } = new(0, 0);

    [Required]
    public PriceAnomalyType AnomalyType { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    public string? Message { get; set; }
}