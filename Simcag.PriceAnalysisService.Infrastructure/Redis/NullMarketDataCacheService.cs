using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

/// <summary>Used when Redis is not configured; analysis still uses HTTP to Market Data.</summary>
public sealed class NullMarketDataCacheService : IMarketDataCacheService
{
    public Task<decimal?> GetPriceAsync(string key) => Task.FromResult<decimal?>(null);

    public Task SetPriceAsync(string key, decimal price) => Task.CompletedTask;
}
