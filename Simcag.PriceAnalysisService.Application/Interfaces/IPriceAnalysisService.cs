using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Events;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceAnalysisService
{
    /// <param name="historicalAveragePrior">
    ///   Average of internal price history for this product <em>before</em> the current line is applied (e.g. from the processing pipeline).
    ///   When null, the service derives context only from <see cref="IPriceRepository"/> (sync/API path).
    /// </param>
    Task<PriceAnalysisResult> AnalyzePriceAsync(
        DataProcessedEvent dataProcessedEvent,
        decimal? historicalAveragePrior,
        CancellationToken ct);
    Task<PriceAnalysisResult> GetLatestProductAnalysisAsync(string productId, CancellationToken cancellationToken);
    Task<IEnumerable<PriceAnomaly>> GetAnomaliesAsync(CancellationToken cancellationToken);
    Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisAsync(CancellationToken cancellationToken);
    Task<PriceAnalysisResult> RecalculatePriceStatsAsync(string productId, CancellationToken cancellationToken);
}