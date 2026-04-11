using System.ComponentModel.DataAnnotations;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class ProductPriceHistory
{
    [Key]
    public string ProductId { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    public string Source { get; set; } = string.Empty;
    
    public string? MarketPriceSource { get; set; }
}