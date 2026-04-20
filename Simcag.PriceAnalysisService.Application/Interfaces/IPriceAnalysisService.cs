using Simcag.PriceAnalysisService.Application.Events;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IPriceAnalysisService
{
    Task AnalyzePriceAsync(DataProcessedEvent dataProcessedEvent, CancellationToken ct);
}