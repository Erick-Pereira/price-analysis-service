namespace Simcag.PriceAnalysisService.Application.Interfaces
{
    public interface IPriceStatisticsService
    {
        decimal CalculateDifferencePercentage(decimal pricePaid, decimal marketPrice);
    }
}