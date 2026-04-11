using Simcag.PriceAnalysisService.Application.Interfaces;

namespace Simcag.PriceAnalysisService.Application.Services
{
    public class PriceOutlierDetectionService : IPriceOutlierDetectionService
    {
        public string Classify(decimal differencePercentage)
        {
            if (differencePercentage > 50)
                return "SUPERFATURADO";

            if (differencePercentage > 20)
                return "SUSPEITO";

            return "NORMAL";
        }
    }
}
