using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.PriceAnalysisService.Domain.Events;
using Simcag.Shared.Messaging.Contracts;

namespace Simcag.PriceAnalysisService.Tests;

public class PriceAnalysisServiceTests
{
    private readonly Mock<IPriceAnalysisRepository> _analysisRepositoryMock;
    private readonly Mock<IEventPublisher<PriceAnalyzedEvent>> _eventPublisherMock;
    private readonly Mock<ILogger<PriceAnalysisService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly PriceAnalysisService _service;

    public PriceAnalysisServiceTests()
    {
        _analysisRepositoryMock = new Mock<IPriceAnalysisRepository>();
        _eventPublisherMock = new Mock<IEventPublisher<PriceAnalyzedEvent>>();
        _loggerMock = new Mock<ILogger<PriceAnalysisService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new PriceAnalysisService(
            _analysisRepositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object,
            _httpClient);
    }

    [Fact]
    public async Task AnalyzePriceAsync_ShouldCreateAnalysis_WhenMarketPriceAvailable()
    {
        // Arrange
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-1",
            ProductName = "Test Product",
            Price = 100m,
            Source = "test-source",
            Market = "test-market",
            OccurredAt = DateTime.UtcNow
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

        SetupHttpMock("/api/marketdata/price?productName=Test+Product", marketResponse);

        // Act
        await _service.AnalyzePriceAsync(dataProcessedEvent, CancellationToken.None);

        // Assert
        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.ProductId == "test-product-1" &&
                a.OriginalPrice == 100m &&
                a.MarketPrice == 90m &&
                a.DeviationPercentage == 11.11m && // (100-90)/90 * 100
                a.IsAnomalous == true), // >10% deviation
            It.IsAny<CancellationToken>()), Times.Once);

        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<PriceAnalyzedEvent>(e =>
                e.ProductId == "test-product-1" &&
                e.AveragePrice == 90m &&
                e.LastPrice == 100m &&
                e.Trend == "UP"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzePriceAsync_ShouldHandleMarketPriceUnavailable()
    {
        // Arrange
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-2",
            ProductName = "Unknown Product",
            Price = 100m,
            Source = "test-source",
            Market = "test-market",
            OccurredAt = DateTime.UtcNow
        };

        SetupHttpMock("/api/marketdata/price?productName=Unknown+Product",
            new { Success = false, Message = "Not found" }, HttpStatusCode.NotFound);

        // Act
        await _service.AnalyzePriceAsync(dataProcessedEvent, CancellationToken.None);

        // Assert
        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.ProductId == "test-product-2" &&
                a.OriginalPrice == 100m &&
                a.MarketPrice == null &&
                a.DeviationPercentage == null &&
                a.IsAnomalous == false &&
                a.AnalysisNotes.Contains("Market price not available")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzePriceAsync_ShouldDetectNormalPriceRange()
    {
        // Arrange
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-3",
            ProductName = "Normal Product",
            Price = 105m,
            Source = "test-source",
            Market = "test-market",
            OccurredAt = DateTime.UtcNow
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

        SetupHttpMock("/api/marketdata/price?productName=Normal+Product", marketResponse);

        // Act
        await _service.AnalyzePriceAsync(dataProcessedEvent, CancellationToken.None);

        // Assert
        _analysisRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceAnalysis>(a =>
                a.DeviationPercentage == 5m && // (105-100)/100 * 100
                a.IsAnomalous == false), // ≤10% deviation
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzePriceAsync_ShouldDetermineTrendCorrectly()
    {
        // Arrange - Price significantly above market (UP trend)
        var dataProcessedEvent = new DataProcessedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = "test-product-4",
            ProductName = "Trending Up Product",
            Price = 120m,
            Source = "test-source",
            Market = "test-market",
            OccurredAt = DateTime.UtcNow
        };

        var marketResponse = new
        {
            Success = true,
            Data = new
            {
                ProductName = "Trending Up Product",
                Price = 100m,
                Source = "MarketData",
                CollectedDate = DateTime.UtcNow
            }
        };

        SetupHttpMock("/api/marketdata/price?productName=Trending+Up+Product", marketResponse);

        // Act
        await _service.AnalyzePriceAsync(dataProcessedEvent, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<PriceAnalyzedEvent>(e => e.Trend == "UP"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupHttpMock(string requestUri, object responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Content = JsonContent.Create(responseContent);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}