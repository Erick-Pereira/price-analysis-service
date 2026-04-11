using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceAnalysisService
{
    Task<PriceAnalysisResult> AnalyzePriceAsync(string productId, CancellationToken cancellationToken = default);
    Task<PriceAnalysisResult> RecalculatePriceStatsAsync(string productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceAnomaly>> GetAnomaliesAsync(CancellationToken cancellationToken = default);
}