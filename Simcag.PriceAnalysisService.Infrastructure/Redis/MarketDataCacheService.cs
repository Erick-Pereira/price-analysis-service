using StackExchange.Redis;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

public class MarketDataCacheService : IMarketDataCacheService
{
    private readonly IDatabase _database;

    public MarketDataCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);

        if (value.HasValue)
        {
            Console.WriteLine($" CACHE HIT: {key}");
            return value;
        }

        Console.WriteLine($" CACHE MISS: {key}");
        return null;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl)
    {
        await _database.StringSetAsync(key, value, ttl);
    }
}