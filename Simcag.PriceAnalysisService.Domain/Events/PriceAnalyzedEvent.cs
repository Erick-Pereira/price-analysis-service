using System;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Application.Events;

public class PriceAnalyzedEvent : BaseEvent
{
    public override string EventType => "price.analyzed";

    public string ProductId { get; init; } = string.Empty;
    public decimal AveragePrice { get; init; }
    public decimal MedianPrice { get; init; }
    public decimal StandardDeviation { get; init; }
    public decimal SafeZoneMin { get; init; }
    public decimal SafeZoneMax { get; init; }
    public DateTime AnalysisDate { get; init; }
    public bool HasAnomalies { get; init; }

    public PriceAnalyzedEvent() { }
}
