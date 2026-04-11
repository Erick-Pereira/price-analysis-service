using System;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Application.Events;

public class DataProcessedEvent : BaseEvent
{
    public override string EventType => "data.processed";

    public string ProductId { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Source { get; init; } = string.Empty;
    public string Market { get; init; } = string.Empty;

    public DataProcessedEvent() { }

    public DataProcessedEvent(Guid eventId, string productId, decimal price, DateTime timestamp, string source, string market)
    {
        EventId = eventId;
        ProductId = productId;
        Price = price;
        Timestamp = timestamp;
        Source = source;
        Market = market;
    }
}
