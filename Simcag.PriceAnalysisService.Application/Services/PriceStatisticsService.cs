using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Application.Services
{
    public class PriceStatisticsService : IPriceStatisticsService
    {
        public decimal CalculateDifferencePercentage(decimal pricePaid, decimal marketPrice)
        {
            return ((pricePaid - marketPrice) / marketPrice) * 100;
        }
    }
}