using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
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
    private readonly Mock<IHttpClientFactory> _httpFactory;
    private readonly Mock<IMarketDataCacheService> _cacheMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
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
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object) { BaseAddress = new Uri("http://localhost:8082") };
        _httpFactory = new Mock<IHttpClientFactory>();
        _httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
        _service = new Service.PriceAnalysisService(
            _analysisRepositoryMock.Object,
            _priceRepositoryMock.Object,
            _loggerMock.Object,
            _httpFactory.Object,
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
        var marketResponse = new
        {
            Success = true,
            Data = new
            {
                ProductName = "Test Product",
                Price = 90m,
                Source = "MarketData",
                CollectedDate = DateTime.UtcNow
            }
        };
        SetupHttpMock("Test Product", marketResponse);

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
        SetupHttpMock("Unknown Product",
            new { Success = false, Message = "Not found" }, HttpStatusCode.NotFound);

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
        var marketResponse = new
        {
            Success = true,
            Data = new
            {
                ProductName = "Normal Product",
                Price = 100m,
                Source = "MarketData",
                CollectedDate = DateTime.UtcNow
            }
        };
        SetupHttpMock("Normal Product", marketResponse);

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
        SetupHttpMock("Expensive", new
        {
            Success = true,
            Data = new
            {
                ProductName = "Expensive",
                Price = 100m,
                Source = "M",
                CollectedDate = DateTime.UtcNow
            }
        });

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
        SetupHttpMock("Crisis", new
        {
            Success = true,
            Data = new
            {
                ProductName = "Crisis",
                Price = 100m,
                Source = "M",
                CollectedDate = DateTime.UtcNow
            }
        });

        await _service.AnalyzePriceAsync(data, null, CancellationToken.None);

        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.DeviationPercentage == 50m &&
                a.Severity == DeviationSeverity.Critical),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupHttpMock(string productNameOrFragment, object responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var enc = Uri.EscapeDataString(productNameOrFragment);
        var response = new HttpResponseMessage(statusCode) { Content = JsonContent.Create(responseContent) };
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null
                    && req.RequestUri.ToString().Contains("marketdata", StringComparison.OrdinalIgnoreCase)
                    && (req.RequestUri.ToString().Contains(enc, StringComparison.Ordinal)
                        || req.RequestUri.ToString().Contains(productNameOrFragment, StringComparison.Ordinal))),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
