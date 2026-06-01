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
        var result = await _priceAnalysisService.GetLatestProductAnalysisAsync(productId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("outliers")]
    public async Task<ActionResult<IEnumerable<PriceAnomaly>>> GetAnomalies(
        CancellationToken cancellationToken = default)
    {
        var anomalies = await _priceAnalysisService.GetAnomaliesAsync(cancellationToken);
        return Ok(anomalies);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<PriceAnalysisResult>>> GetAllAnalysis(
        CancellationToken cancellationToken = default)
    {
        var results = await _priceAnalysisService.GetAllAnalysisAsync(cancellationToken);
        return Ok(results);
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