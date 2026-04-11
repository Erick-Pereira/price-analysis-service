using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceOutlierDetectionService
{
    Task<IEnumerable<PriceAnomaly>> DetectAnomaliesAsync(
        decimal currentPrice,
        ProductPriceAggregate aggregate,
        CancellationToken cancellationToken = default);
}