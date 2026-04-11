using Simcag.PriceAnalysisService.Application.Interfaces;
using System.Collections.Generic;

namespace Simcag.PriceAnalysisService.Infrastructure.Repositories
{
    public class InMemoryPriceRepository : IPriceRepository
    {
        private static List<object> _data = new List<object>();

        public void Save(object analysis)
        {
            _data.Add(analysis);
        }

        public List<object> GetAll()
        {
            return _data;
        }
    }
}