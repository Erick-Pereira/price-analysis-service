using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

public class RedisEventPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly string _streamName;

    public RedisEventPublisher(IConnectionMultiplexer redis, string streamName)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _streamName = streamName;
    }

    public async Task PublishAsync(string eventName, object eventData, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(eventData);
        var message = new NameValueEntry[]
        {
            new("event", eventName),
            new("data", json),
            new("timestamp", DateTime.UtcNow.ToString("o"))
        };

        await _database.StreamAddAsync(_streamName, message);
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}