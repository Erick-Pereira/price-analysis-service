namespace Simcag.PriceAnalysisService.Application.Interfaces
{
    public interface IPriceAnalysisService
    {
        object Analyze(string name, decimal pricePaid);
    }
}