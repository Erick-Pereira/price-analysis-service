using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Enums;

namespace Simcag.PriceAnalysisService.Application.Services;

public class PriceOutlierDetectionService : IPriceOutlierDetectionService
{
    public Task<IEnumerable<PriceAnomaly>> DetectAnomaliesAsync(
        decimal currentPrice,
        ProductPriceAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<PriceAnomaly>();
        var safeZone = aggregate.SafeZone;

        if (currentPrice > safeZone.Max)
        {
            anomalies.Add(new PriceAnomaly
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = aggregate.ProductId,
                CurrentPrice = currentPrice,
                AveragePrice = aggregate.AveragePrice,
                AllowedRange = safeZone,
                AnomalyType = PriceAnomalyType.HighAnomaly,
                Timestamp = DateTime.UtcNow,
                Message = $"Price {currentPrice:N2} exceeds safe zone maximum of {safeZone.Max:N2}"
            });
        }
        else if (currentPrice < safeZone.Min)
        {
            anomalies.Add(new PriceAnomaly
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = aggregate.ProductId,
                CurrentPrice = currentPrice,
                AveragePrice = aggregate.AveragePrice,
                AllowedRange = safeZone,
                AnomalyType = PriceAnomalyType.LowAnomaly,
                Timestamp = DateTime.UtcNow,
                Message = $"Price {currentPrice:N2} falls below safe zone minimum of {safeZone.Min:N2}"
            });
        }

        return Task.FromResult<IEnumerable<PriceAnomaly>>(anomalies);
    }
}