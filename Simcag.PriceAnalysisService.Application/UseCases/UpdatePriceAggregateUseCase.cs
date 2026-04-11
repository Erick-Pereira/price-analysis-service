using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Application.UseCases;

public sealed class UpdatePriceAggregateUseCase
{
    private readonly IPriceAnalysisService _priceAnalysisService;

    public UpdatePriceAggregateUseCase(IPriceAnalysisService priceAnalysisService)
    {
        _priceAnalysisService = priceAnalysisService;
    }

    public Task<PriceAnalysisResult> ExecuteAsync(string productId, CancellationToken cancellationToken = default)
    {
        return _priceAnalysisService.RecalculatePriceStatsAsync(productId, cancellationToken);
    }
}
