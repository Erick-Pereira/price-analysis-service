using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

public sealed class RedisMarketDataCacheService : IMarketDataCacheService
{
    private const int TtlHours = 1;
    private readonly IDatabase _database;
    private readonly ILogger<RedisMarketDataCacheService> _logger;

    public RedisMarketDataCacheService(IConnectionMultiplexer redis, ILogger<RedisMarketDataCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<decimal?> GetPriceAsync(string cacheKey)
    {
        var redisKey = (RedisKey)$"md:price:{cacheKey}";
        var value = await _database.StringGetAsync(redisKey);

        if (!value.HasValue)
        {
            _logger.LogDebug("Market cache miss for {Key}", cacheKey);
            return null;
        }

        _logger.LogDebug("Market cache hit for {Key}", cacheKey);
        var text = value.ToString();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public async Task SetPriceAsync(string cacheKey, decimal price)
    {
        var redisKey = (RedisKey)$"md:price:{cacheKey}";
        await _database.StringSetAsync(
            redisKey,
            (RedisValue)price.ToString(CultureInfo.InvariantCulture),
            TimeSpan.FromHours(TtlHours));
        _logger.LogDebug("Market cache set {Key} = {Price}", cacheKey, price);
    }
}
