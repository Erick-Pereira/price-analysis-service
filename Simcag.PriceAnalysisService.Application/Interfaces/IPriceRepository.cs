using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceRepository
{
    Task<List<PriceHistory>> GetPriceHistoryAsync(string productId, CancellationToken cancellationToken = default);
    Task<PriceAnalysisResult?> GetAnalysisResultAsync(string productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisResultsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnomaly>> GetAllAnomaliesAsync(CancellationToken cancellationToken = default);
    Task SaveAnalysisResultAsync(PriceAnalysisResult analysisResult, CancellationToken cancellationToken = default);
    Task SaveAnomalyAsync(PriceAnomaly anomaly, CancellationToken cancellationToken = default);
    Task AddPriceHistoryAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkEventAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
