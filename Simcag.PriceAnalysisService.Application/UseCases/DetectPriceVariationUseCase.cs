using System;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Enums;

namespace Simcag.PriceAnalysisService.Application.UseCases;

public sealed class DetectPriceVariationUseCase
{
    public PriceAnomaly? Execute(PriceAnalysisResult analysisResult, decimal currentPrice)
    {
        if (analysisResult == null)
            throw new ArgumentNullException(nameof(analysisResult));

        if (!analysisResult.SafeZone.Contains(currentPrice))
        {
            return new PriceAnomaly
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = analysisResult.ProductId,
                CurrentPrice = currentPrice,
                AveragePrice = analysisResult.AveragePrice,
                AllowedRange = analysisResult.SafeZone,
                AnomalyType = currentPrice > analysisResult.SafeZone.Max
                    ? PriceAnomalyType.HighAnomaly
                    : PriceAnomalyType.LowAnomaly,
                Timestamp = DateTime.UtcNow,
                Message = $"Price {currentPrice:N2} is outside safe zone {analysisResult.SafeZone}."
            };
        }

        return null;
    }
}
