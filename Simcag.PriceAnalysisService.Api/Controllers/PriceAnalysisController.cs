using Microsoft.AspNetCore.Mvc;
using Npgsql;
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
            var result = await _priceAnalysisService.GetLatestProductAnalysisAsync(productId, cancellationToken);
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
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            _logger.LogWarning(ex, "Database unavailable while retrieving analysis results");
            return StatusCode(503, new { error = "Database unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all analysis results");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static bool IsDatabaseUnavailable(Exception ex)
    {
        // Npgsql wraps connection failures as transient InvalidOperationException sometimes.
        if (ex is NpgsqlException)
            return true;
        if (ex is InvalidOperationException && ex.InnerException is NpgsqlException)
            return true;

        var cur = ex;
        while (cur != null)
        {
            if (cur is NpgsqlException)
                return true;
            cur = cur.InnerException;
        }

        return false;
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

        // Alinhado ao limiar do serviço (15% / 30%)
        string status;
        var absD = Math.Abs(difference);
        if (absD > 30)
            status = "CRITICO";
        else if (absD > 15)
            status = "ALERTA";
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

public sealed class PriceRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }
}