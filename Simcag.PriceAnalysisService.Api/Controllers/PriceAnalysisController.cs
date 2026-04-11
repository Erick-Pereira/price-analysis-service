using Microsoft.AspNetCore.Mvc;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Domain.Entities;

namespace Simcag.PriceAnalysisService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceAnalysisController : ControllerBase
{
    private readonly IPriceAnalysisService _priceAnalysisService;
    private readonly ILogger<PriceAnalysisController> _logger;

    public PriceAnalysisController(
        IPriceAnalysisService priceAnalysisService,
        ILogger<PriceAnalysisController> logger)
    {
        _priceAnalysisService = priceAnalysisService;
        _logger = logger;
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<PriceAnalysisResult>> GetProductAnalysis(
        string productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _priceAnalysisService.AnalyzePriceAsync(productId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Insufficient data for product {ProductId}", productId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing price for product {ProductId}", productId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("outliers")]
    public async Task<ActionResult<IEnumerable<PriceAnomaly>>> GetAnomalies(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var anomalies = await _priceAnalysisService.GetAnomaliesAsync(cancellationToken);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomalies");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<PriceAnalysisResult>>> GetAllAnalysis(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _priceAnalysisService.GetAllAnalysisAsync(cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all analysis results");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] PriceRequest request)
    {
        // 🔥 Tabela mock de preços de mercado
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "Notebook", 4000 },
                { "Mouse", 50 },
                { "Teclado", 150 },
                { "Monitor", 800 }
            };

        // 🔍 Busca preço de mercado
        decimal marketPrice = prices.ContainsKey(request.Name)
            ? prices[request.Name]
            : 100;

        // 📊 Cálculo da diferença (%)
        var difference = ((request.PricePaid - marketPrice) / marketPrice) * 100;

        // 🚨 Classificação
        string status;

        if (difference > 50)
            status = "SUPERFATURADO";
        else if (difference > 20)
            status = "SUSPEITO";
        else
            status = "NORMAL";

        // 📦 Resposta
        var response = new
        {
            product = request.Name,
            pricePaid = request.PricePaid,
            marketPrice = marketPrice,
            difference = Math.Round(difference, 2),
            status = status
        };

        return Ok(response);
    }
}

// 📥 Modelo correto da requisição
public class PriceRequest
{
    public string Name { get; set; }
    public decimal PricePaid { get; set; }
}