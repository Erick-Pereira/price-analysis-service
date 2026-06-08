using Microsoft.Extensions.Logging;
using Moq;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Application.Services;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Enums;
using Simcag.PriceAnalysisService.Domain.Events;
using Service = Simcag.PriceAnalysisService.Application.Services;

namespace Simcag.PriceAnalysisService.Tests;

public class PriceAnalysisServiceTests
{
    private readonly Mock<IPriceAnalysisRepository> _analysisRepositoryMock;
    private readonly Mock<IPriceRepository> _priceRepositoryMock;
    private readonly Mock<ILogger<Service.PriceAnalysisService>> _loggerMock;
    private readonly Mock<IMarketDataPriceClient> _marketDataClientMock;
    private readonly Mock<IMarketDataCacheService> _cacheMock;
    private readonly Service.PriceAnalysisService _service;

    public PriceAnalysisServiceTests()
    {
        _analysisRepositoryMock = new Mock<IPriceAnalysisRepository>();
        _priceRepositoryMock = new Mock<IPriceRepository>();
        _priceRepositoryMock
            .Setup(x => x.GetPriceHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());
        _cacheMock = new Mock<IMarketDataCacheService>();
        _cacheMock
            .Setup(c => c.GetPriceAsync(It.IsAny<string>()))
            .ReturnsAsync((decimal?)null);
        _loggerMock = new Mock<ILogger<Service.PriceAnalysisService>>();
        _marketDataClientMock = new Mock<IMarketDataPriceClient>();
        _service = new Service.PriceAnalysisService(
            _analysisRepositoryMock.Object,
            _priceRepositoryMock.Object,
            _loggerMock.Object,
            _marketDataClientMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task AnalyzePriceAsync_SmallDeviation_Vs_Market_IsNormal_At_15_Threshold()
    {
        // 11.1% is below WARNING (>15%)
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-1",
            ProductName = "Test Product",
            Price = 100m,
            Source = "test-source",
            Market = "test-market"
        };
        SetupMarketDataMock("Test Product", 90m);

        var result = await _service.AnalyzePriceAsync(dataProcessedEvent, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.ProductId == "test-product-1" &&
                a.OriginalPrice == 100m &&
                a.MarketPrice == 90m &&
                a.Severity == DeviationSeverity.Normal &&
                a.IsAnomalous == false),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(DeviationSeverity.Normal, result.Severity);
    }

    [Fact]
    public async Task AnalyzePriceAsync_NoReference_ComputesNoDeviation()
    {
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-2",
            ProductName = "Unknown Product",
            Price = 100m,
            Source = "test-source",
            Market = "test-market"
        };
        SetupMarketDataMock("Unknown Product", null);

        var result = await _service.AnalyzePriceAsync(dataProcessedEvent, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.ProductId == "test-product-2" &&
                a.OriginalPrice == 100m &&
                a.MarketPrice == null &&
                a.DeviationPercentage == null &&
                a.Severity == DeviationSeverity.Normal &&
                a.IsAnomalous == false),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(DeviationSeverity.Normal, result.Severity);
    }

    [Fact]
    public async Task AnalyzePriceAsync_FivePercent_IsNormal()
    {
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-3",
            ProductName = "Normal Product",
            Price = 105m,
            Source = "test-source",
            Market = "test-market"
        };
        SetupMarketDataMock("Normal Product", 100m);

        await _service.AnalyzePriceAsync(dataProcessedEvent, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a => a.DeviationPercentage == 5m && a.Severity == DeviationSeverity.Normal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzePriceAsync_Warning_When_Market_Deviation_Above_15()
    {
        var data = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "p1",
            ProductName = "Expensive",
            Price = 120m,
            Source = "s",
            Market = "m"
        };
        SetupMarketDataMock("Expensive", 100m);

        await _service.AnalyzePriceAsync(data, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.DeviationPercentage == 20m &&
                a.Severity == DeviationSeverity.Warning &&
                a.IsAnomalous == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzePriceAsync_Critical_When_Market_Deviation_Above_30()
    {
        var data = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "p2",
            ProductName = "Crisis",
            Price = 150m,
            Source = "s",
            Market = "m"
        };
        SetupMarketDataMock("Crisis", 100m);

        await _service.AnalyzePriceAsync(data, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.DeviationPercentage == 50m &&
                a.Severity == DeviationSeverity.Critical),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupMarketDataMock(string productName, decimal? price)
    {
        _marketDataClientMock
            .Setup(c => c.LookupPriceAsync(
                It.Is<string>(n => n.Contains(productName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(price.HasValue
                ? new MarketPriceLookupResult
                {
                    Price = price.Value,
                    Source = "MarketData",
                }
                : null);
    }
}
