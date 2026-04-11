using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.ValueObjects;

namespace Simcag.PriceAnalysisService.Application.Services;

public class PriceStatisticsService : IPriceStatisticsService
{
    public async Task<PriceStatistics> CalculateStatisticsAsync(
        IEnumerable<decimal> prices,
        CancellationToken cancellationToken = default)
    {
        var priceList = prices.ToList();

        if (!priceList.Any())
            throw new ArgumentException("Price list cannot be empty", nameof(prices));

        var average = priceList.Average();
        var median = CalculateMedian(priceList);
        var standardDeviation = CalculateStandardDeviation(priceList, average);

        return new PriceStatistics(average, median, standardDeviation);
    }

    public Task<PriceRange> CalculateSafeZoneAsync(
        decimal averagePrice,
        CancellationToken cancellationToken = default)
    {
        var min = averagePrice * 0.8m;
        var max = averagePrice * 1.2m;

        return Task.FromResult(new PriceRange(min, max));
    }

    private static decimal CalculateMedian(List<decimal> prices)
    {
        var sorted = prices.OrderBy(p => p).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
        {
            var midIndex = count / 2;
            return (sorted[midIndex - 1] + sorted[midIndex]) / 2;
        }

        return sorted[count / 2];
    }

    private static decimal CalculateStandardDeviation(List<decimal> prices, decimal average)
    {
        var variance = prices.Average(p => (p - average) * (p - average));
        return (decimal)Math.Sqrt((double)variance);
    }
}
