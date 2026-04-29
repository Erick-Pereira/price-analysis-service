using Simcag.Shared.Events;
using Simcag.Shared.Messaging;

namespace Simcag.PriceAnalysisService.Domain.Events;

public class PriceAnalyzedEvent : BaseEvent
{
    public override string EventType => EventNames.PriceAnalyzed;

    public Guid? ExpenseId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string SupplierId { get; init; } = string.Empty;
    public decimal LastPrice { get; init; }
    /// <summary>Market reference used for the primary comparison (from Market Data Service).</summary>
    public decimal MarketAverage { get; init; }
    /// <summary>Condominium / internal history average prior to the current line item.</summary>
    public decimal HistoricalAverage { get; init; }
    /// <summary>Same as <see cref="PriceVariation"/>, explicit name for audit.</summary>
    public decimal DeviationPercentage { get; init; }
    public decimal PriceVariation { get; init; }
    public string Severity { get; init; } = "NORMAL";
    public decimal AveragePrice { get; init; }
    public decimal MedianPrice { get; init; }
    public decimal StandardDeviation { get; init; }
    public decimal SafeZoneMin { get; init; }
    public decimal SafeZoneMax { get; init; }
    public string Trend { get; init; } = string.Empty;
    public DateTime AnalysisDate { get; init; }
    public bool HasAnomalies { get; init; }

    public PriceAnalyzedEvent() { }
}
