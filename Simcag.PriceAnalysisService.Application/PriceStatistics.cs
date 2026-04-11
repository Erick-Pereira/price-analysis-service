namespace Simcag.PriceAnalysisService.Application;

public class PriceStatistics
{
    public decimal Average { get; set; }
    public decimal Median { get; set; }
    public decimal StandardDeviation { get; set; }

    public PriceStatistics() { }

    public PriceStatistics(decimal average, decimal median, decimal standardDeviation)
    {
        Average = average;
        Median = median;
        StandardDeviation = standardDeviation;
    }
}