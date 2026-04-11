using Simcag.PriceAnalysisService.Application.Interfaces;
using System;
using System.Collections.Generic;

namespace Simcag.PriceAnalysisService.Application.Services
{
    public class PriceAnalysisService : IPriceAnalysisService
    {
        private readonly IPriceStatisticsService _statisticsService;
        private readonly IPriceOutlierDetectionService _outlierService;
        private readonly IPriceRepository _repository;

        public PriceAnalysisService(
            IPriceStatisticsService statisticsService,
            IPriceOutlierDetectionService outlierService,
            IPriceRepository repository)
        {
            _statisticsService = statisticsService;
            _outlierService = outlierService;
            _repository = repository;
        }

        public object Analyze(string name, decimal pricePaid)
        {
            var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "Notebook", 4000 },
                { "Mouse", 50 },
                { "Teclado", 150 },
                { "Monitor", 800 }
            };

            decimal marketPrice = prices.ContainsKey(name)
                ? prices[name]
                : 100;

            var difference = _statisticsService.CalculateDifferencePercentage(pricePaid, marketPrice);
            var status = _outlierService.Classify(difference);

            var result = new
            {
                product = name,
                pricePaid = pricePaid,
                marketPrice = marketPrice,
                difference = Math.Round(difference, 2),
                status = status
            };

            // 🔥 AGORA SALVA
            _repository.Save(result);

            return result;
        }

        // 🔥 NOVO MÉTODO (pra buscar histórico)
        public List<object> GetAll()
        {
            return _repository.GetAll();
        }
    }
}