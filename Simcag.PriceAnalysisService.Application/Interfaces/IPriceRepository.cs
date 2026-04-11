namespace Simcag.PriceAnalysisService.Application.Interfaces
{
    public interface IPriceRepository
    {
        void Save(object analysis);
        List<object> GetAll();
    }
}