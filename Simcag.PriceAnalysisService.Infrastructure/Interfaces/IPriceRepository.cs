using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Infrastructure.Interfaces;

public interface IPriceRepository
{
    Task<List<ProductPriceHistory>> GetPriceHistoryAsync(string productId, CancellationToken cancellationToken = default);
    Task<PriceAnalysisResult> GetAnalysisResultAsync(string productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisResultsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnomaly>> GetAllAnomaliesAsync(CancellationToken cancellationToken = default);
    Task SaveAnalysisResultAsync(PriceAnalysisResult analysisResult, CancellationToken cancellationToken = default);
    Task SaveAnomalyAsync(PriceAnomaly anomaly, CancellationToken cancellationToken = default);
}