using Simcag.PriceAnalysisService.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceAnalysisRepository
{
    Task<PriceAnalysis?> GetLatestByProductIdAsync(string productId, CancellationToken ct);
    Task AddAsync(PriceAnalysis analysis, CancellationToken ct);
    Task<IEnumerable<PriceAnalysis>> GetAnomaliesAsync(int page, int pageSize, CancellationToken ct);
    Task<int> GetAnomaliesCountAsync(CancellationToken ct);
}