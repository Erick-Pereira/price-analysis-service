using System;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging;

namespace Simcag.PriceAnalysisService.Domain.Events;

/// <summary>
/// Internal DTO for analysis after <see cref="PriceDataProcessedEvent"/> (or other sources) is mapped.
/// </summary>
public class DataProcessedEvent : BaseEvent
{
    public override string EventType => EventNames.DataProcessed;

    public Guid? ExpenseId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string SupplierId { get; init; } = string.Empty;
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
