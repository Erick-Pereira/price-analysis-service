using Simcag.PriceAnalysisService.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class ProductPriceAggregate
{
    [Key]
    public string ProductId { get; private set; } = string.Empty;

    [Required]
    public decimal AveragePrice { get; private set; }

    [Required]
    public decimal MedianPrice { get; private set; }

    [Required]
    public decimal StandardDeviation { get; private set; }

    [Required]
    public PriceRange SafeZone { get; private set; }

    [Required]
    public DateTime LastAnalyzedAt { get; private set; }

    private ProductPriceAggregate()
    {
    }

    public static ProductPriceAggregate FromHistory(string productId, IReadOnlyCollection<PriceHistory> history)
    {
        if (history == null || history.Count < 5)
            throw new InvalidOperationException("At least 5 price records are required to build an aggregate.");

        var prices = history.Select(x => x.Price).OrderBy(x => x).ToList();
        var average = prices.Average();
        var median = prices.Count % 2 == 0
            ? (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2
            : prices[prices.Count / 2];

        var variance = prices.Average(p => Math.Pow((double)(p - average), 2));
        var standardDeviation = (decimal)Math.Sqrt(variance);
        var safeZone = new PriceRange(average - standardDeviation * 2, average + standardDeviation * 2);

        return new ProductPriceAggregate
        {
            ProductId = productId,
            AveragePrice = average,
            MedianPrice = median,
            StandardDeviation = standardDeviation,
            SafeZone = safeZone,
            LastAnalyzedAt = DateTime.UtcNow
        };
    }

    public bool IsOutsideSafeZone(decimal currentPrice) => !SafeZone.Contains(currentPrice);
}