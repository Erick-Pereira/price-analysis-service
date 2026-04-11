namespace Simcag.PriceAnalysisService.Application.Interfaces
{
    public interface IPriceOutlierDetectionService
    {
        string Classify(decimal differencePercentage);
    }
}