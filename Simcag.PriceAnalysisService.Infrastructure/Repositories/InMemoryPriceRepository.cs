using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Infrastructure.Repositories;

public class InMemoryPriceRepository : IPriceRepository
{
    private static readonly Dictionary<string, List<PriceHistory>> _priceHistory = new();
    private static readonly List<PriceAnalysisResult> _analysisResults = new();
    private static readonly List<PriceAnomaly> _anomalies = new();
    private static readonly HashSet<Guid> _processedEvents = new();

    public Task<List<PriceHistory>> GetPriceHistoryAsync(string productId, CancellationToken cancellationToken = default)
    {
        if (_priceHistory.TryGetValue(productId, out var history))
            return Task.FromResult(history);

        return Task.FromResult(new List<PriceHistory>());
    }

    public Task<PriceAnalysisResult?> GetAnalysisResultAsync(string productId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PriceAnalysisResult?>(_analysisResults.FirstOrDefault(r => r.ProductId == productId));
    }

    public Task<IEnumerable<PriceAnalysisResult>> GetAllAnalysisResultsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<PriceAnalysisResult>>(_analysisResults);
    }

    public Task<IEnumerable<PriceAnomaly>> GetAllAnomaliesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<PriceAnomaly>>(_anomalies);
    }

    public Task SaveAnalysisResultAsync(PriceAnalysisResult analysisResult, CancellationToken cancellationToken = default)
    {
        var existing = _analysisResults.FirstOrDefault(r => r.ProductId == analysisResult.ProductId);
        if (existing != null)
        {
            _analysisResults.Remove(existing);
        }

        _analysisResults.Add(analysisResult);
        return Task.CompletedTask;
    }

    public Task SaveAnomalyAsync(PriceAnomaly anomaly, CancellationToken cancellationToken = default)
    {
        _anomalies.Add(anomaly);
        return Task.CompletedTask;
    }

    public Task AddPriceHistoryAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default)
    {
        if (!_priceHistory.TryGetValue(priceHistory.ProductId, out var history))
        {
            history = new List<PriceHistory>();
            _priceHistory[priceHistory.ProductId] = history;
        }

        history.Add(priceHistory);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_processedEvents.Contains(eventId));
    }

    public Task MarkEventAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        _processedEvents.Add(eventId);
        return Task.CompletedTask;
    }
}
