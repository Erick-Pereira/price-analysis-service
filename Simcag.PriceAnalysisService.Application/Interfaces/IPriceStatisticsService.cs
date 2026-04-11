using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.ValueObjects;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceStatisticsService
{
    Task<PriceStatistics> CalculateStatisticsAsync(IEnumerable<decimal> prices, CancellationToken cancellationToken = default);

    Task<PriceRange> CalculateSafeZoneAsync(decimal averagePrice, CancellationToken cancellationToken = default);
}