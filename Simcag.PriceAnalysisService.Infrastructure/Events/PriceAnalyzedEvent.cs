using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Infrastructure.Events
{
    public class PriceAnalyzedEvent : BaseEvent
    {
        public override string EventType => "price.analyzed";

        public string ProductId { get; set; } = string.Empty;
        public decimal AveragePrice { get; set; }
        public decimal MedianPrice { get; set; }
        public decimal StandardDeviation { get; set; }
        public decimal SafeZoneMin { get; set; }
        public decimal SafeZoneMax { get; set; }
        public DateTime AnalysisDate { get; set; }
        public bool HasAnomalies { get; set; }

        public PriceAnalyzedEvent()
        {
        }

        public PriceAnalyzedEvent(string productId, decimal averagePrice, decimal medianPrice, decimal standardDeviation, decimal safeZoneMin, decimal safeZoneMax, DateTime analysisDate, bool hasAnomalies)
        {
            ProductId = productId;
            AveragePrice = averagePrice;
            MedianPrice = medianPrice;
            StandardDeviation = standardDeviation;
            SafeZoneMin = safeZoneMin;
            SafeZoneMax = safeZoneMax;
            AnalysisDate = analysisDate;
            HasAnomalies = hasAnomalies;
        }
    }
}