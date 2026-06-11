using System.Linq;
using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.Shared.Messaging.Rpc;
using Simcag.Shared.Messaging.Rpc.Contracts;

namespace Simcag.PriceAnalysisService.Infrastructure.MarketData;

public sealed class MarketDataPriceRpcClient : IMarketDataPriceClient
{
    private readonly IRabbitMqRpcClient _rpc;
    private readonly ILogger<MarketDataPriceRpcClient> _logger;
    private readonly TimeSpan _timeout;

    public MarketDataPriceRpcClient(IRabbitMqRpcClient rpc, ILogger<MarketDataPriceRpcClient> logger)
    {
        _rpc = rpc;
        _logger = logger;
        _timeout = ReadTimeout();
    }

    public async Task<MarketPriceLookupResult?> LookupPriceAsync(
        string productName,
        decimal declaredReferenceBrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return null;

        try
        {
            var response = await _rpc.RequestAsync<GetMarketPriceRpcRequest, GetMarketPriceRpcResponse>(
                RpcQueues.MarketDataGetPrice,
                new GetMarketPriceRpcRequest
                {
                    ProductName = productName.Trim(),
                    DeclaredReferenceBrl = declaredReferenceBrl,
                },
                _timeout,
                cancellationToken);

            if (response is not { Found: true })
                return null;

            return new MarketPriceLookupResult
            {
                Price = response.Price,
                Source = response.Source,
                BenchmarkKind = response.BenchmarkKind,
                BenchmarkStatus = response.BenchmarkStatus,
                Confidence = response.Confidence,
                SampleCount = response.SampleCount,
                RelativeSpread = response.RelativeSpread,
                SearchQueryUsed = response.SearchQueryUsed,
                CollectedDate = response.CollectedDate,
                BenchmarkRejectionTrail = response.BenchmarkRejectionTrail,
                Evidence = response.BenchmarkDiagnostics?
                    .Select(d => new MarketPriceEvidenceItem
                    {
                        Scope = d.Scope,
                        Phase = d.Phase,
                        Message = d.Message,
                        Detail = d.Detail,
                    })
                    .ToList(),
                ReferenceLinks = response.ReferenceLinks?
                    .Select(l => new MarketPriceReferenceLinkItem { Label = l.Label, Url = l.Url })
                    .ToList(),
                MarketSamples = response.MarketSamples?
                    .Select(s => new MarketPriceSampleItem
                    {
                        Label = s.Label,
                        Url = s.Url,
                        PriceBrl = s.PriceBrl,
                        Provider = s.Provider,
                    })
                    .ToList(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC market-data.get-price failed for {ProductName}", productName);
            return null;
        }
    }

    private static TimeSpan ReadTimeout()
    {
        var raw = Environment.GetEnvironmentVariable("MARKET_DATA_RPC_TIMEOUT_SECONDS")
            ?? Environment.GetEnvironmentVariable("MARKET_DATA__RPC_TIMEOUT_SECONDS");
        if (int.TryParse(raw, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);
        return TimeSpan.FromSeconds(90);
    }
}
