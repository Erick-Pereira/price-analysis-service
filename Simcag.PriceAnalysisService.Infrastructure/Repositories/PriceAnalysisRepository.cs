using Microsoft.EntityFrameworkCore;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Infrastructure.Persistence.DbContext;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Infrastructure.Repositories;

public class PriceAnalysisRepository : IPriceAnalysisRepository
{
    private readonly PriceAnalysisDbContext _dbContext;

    public PriceAnalysisRepository(PriceAnalysisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriceAnalysis?> GetLatestByProductIdAsync(string productId, CancellationToken ct)
    {
        return await _dbContext.PriceAnalyses
            .AsNoTracking()
            .Where(pa => pa.ProductId == productId)
            .OrderByDescending(pa => pa.AnalysisDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(PriceAnalysis analysis, CancellationToken ct)
    {
        await _dbContext.PriceAnalyses.AddAsync(analysis, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<PriceAnalysis>> GetAnomaliesAsync(int page, int pageSize, CancellationToken ct)
    {
        return await _dbContext.PriceAnalyses
            .AsNoTracking()
            .Where(pa => pa.IsAnomalous)
            .OrderByDescending(pa => pa.AnalysisDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetAnomaliesCountAsync(CancellationToken ct)
    {
        return await _dbContext.PriceAnalyses
            .AsNoTracking()
            .CountAsync(pa => pa.IsAnomalous, ct);
    }
}