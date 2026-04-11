using System;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Application.Events;

public class PriceUpdatedEvent : BaseEvent
{
    public override string EventType => "price.updated";

    public string ProductId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public PriceUpdatedEvent() { }
}
