using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis
{
    public class RedisMarketDataCacheService : IMarketDataCacheService
    {
        private readonly IDatabase _database;
        private const int TTL_HOURS = 1;

        public RedisMarketDataCacheService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<decimal?> GetPriceAsync(string productId)
        {
            var value = await _database.StringGetAsync(productId);

            if (!value.HasValue)
            {
                Console.WriteLine($"[CACHE MISS] Produto: {productId}");
                return null;
            }

            Console.WriteLine($"[CACHE HIT] Produto: {productId}");
            return (decimal)value;
        }

        public async Task SetPriceAsync(string productId, decimal price)
        {
            await _database.StringSetAsync(
                productId,
                price,
                TimeSpan.FromHours(TTL_HOURS)
            );

            Console.WriteLine($"[CACHE SET] Produto: {productId} | Preço: {price}");
        }
    }
}