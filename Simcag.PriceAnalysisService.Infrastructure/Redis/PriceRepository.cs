using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

public class PriceRepository : IPriceRepository
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private const string ProcessedEventSetKey = "price-analysis:processed-events";
    private const string AnomaliesKey = "price-analysis:anomalies";

    public PriceRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    private static string PriceHistoryKey(string productId) => $"price-history:{productId}";
    private static string AnalysisResultKey(string productId) => $"analysis-result:{productId}";

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string productId, CancellationToken cancellationToken = default)
    {
        var values = await _database.ListRangeAsync(PriceHistoryKey(productId));

        return values
            .Select(v => JsonConvert.DeserializeObject<PriceHistory>(v)!)
            .Where(x => x != null)
            .ToList()!;
    }

    public async Task<PriceAnalysisResult?> GetAnalysisResultAsync(string productId, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(AnalysisResultKey(productId));

        if (value.IsNullOrEmpty)
            return null;

        return JsonConvert.DeserializeObject<PriceAnalysisResult>(value!);
    }

    public async Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisResultsAsync(CancellationToken cancellationToken = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "analysis-result:*");
        var results = new List<PriceAnalysisResult>();

        foreach (var key in keys)
        {
            var value = await _database.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                var result = JsonConvert.DeserializeObject<PriceAnalysisResult>(value!);
                if (result != null)
                    results.Add(result);
            }
        }

        return results;
    }

    public async Task<IEnumerable<PriceAnomaly>> GetAllAnomaliesAsync(CancellationToken cancellationToken = default)
    {
        var values = await _database.ListRangeAsync(AnomaliesKey);
        return values
            .Select(v => JsonConvert.DeserializeObject<PriceAnomaly>(v)!)
            .Where(x => x != null)
            .ToList()!;
    }

    public Task SaveAnalysisResultAsync(PriceAnalysisResult analysisResult, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(analysisResult);
        return _database.StringSetAsync(AnalysisResultKey(analysisResult.ProductId), json);
    }

    public Task SaveAnomalyAsync(PriceAnomaly anomaly, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(anomaly);
        return _database.ListRightPushAsync(AnomaliesKey, json);
    }

    public Task AddPriceHistoryAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(priceHistory);
        return _database.ListRightPushAsync(PriceHistoryKey(priceHistory.ProductId), json);
    }

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _database.SetContainsAsync(ProcessedEventSetKey, eventId.ToString());
    }

    public Task MarkEventAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _database.SetAddAsync(ProcessedEventSetKey, eventId.ToString());
    }
}