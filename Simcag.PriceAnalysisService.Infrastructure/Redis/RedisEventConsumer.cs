using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Simcag.PriceAnalysisService.Infrastructure.Redis;

public class RedisEventConsumer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _streamName;
    private readonly string _consumerGroup;
    private readonly string _consumerName;

    public RedisEventConsumer(
        IConnectionMultiplexer redis,
        string streamName,
        string consumerGroup,
        string consumerName)
    {
        _redis = redis;
        _streamName = streamName;
        _consumerGroup = consumerGroup;
        _consumerName = consumerName;
    }

    public async Task CreateConsumerGroupIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        await _redis.GetDatabase()
            .StreamCreateConsumerGroupAsync(_streamName, _consumerGroup, createStream: true);
    }

    public async IAsyncEnumerable<StreamEntry> ReadMessagesAsync(
        int count = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = await _redis.GetDatabase()
            .StreamReadGroupAsync(_streamName, _consumerGroup, _consumerName, count: count);

        foreach (var message in messages)
        {
            yield return message;
        }
    }

    public async Task AcknowledgeMessageAsync(StreamEntry message, CancellationToken cancellationToken = default)
    {
        await _redis.GetDatabase()
            .StreamAcknowledgeAsync(_streamName, _consumerGroup, message.Id);
    }
}