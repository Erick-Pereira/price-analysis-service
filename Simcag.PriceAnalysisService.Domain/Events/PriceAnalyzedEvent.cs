using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Domain.Events;

public class PriceAnalyzedEvent : BaseEvent
{
    public override string EventType => "price.analyzed";

    public string ProductId { get; init; } = string.Empty;
    public decimal AveragePrice { get; init; }
    public decimal LastPrice { get; init; }
    public decimal PriceVariation { get; init; }
    public string Trend { get; init; } = string.Empty;

    public PriceAnalyzedEvent() { }
}
