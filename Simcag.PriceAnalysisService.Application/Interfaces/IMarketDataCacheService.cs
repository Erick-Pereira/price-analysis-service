using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Application.Interfaces
{
    public interface IMarketDataCacheService
    {
        Task<decimal?> GetPriceAsync(string productId);
        Task SetPriceAsync(string productId, decimal price);
    }
}