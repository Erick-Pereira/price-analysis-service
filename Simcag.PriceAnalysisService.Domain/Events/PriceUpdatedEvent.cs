using System;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging;

namespace Simcag.PriceAnalysisService.Domain.Events;

public class PriceUpdatedEvent : BaseEvent
{
    public override string EventType => EventNames.PriceUpdated;

    public string ProductId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public PriceUpdatedEvent() { }
}
