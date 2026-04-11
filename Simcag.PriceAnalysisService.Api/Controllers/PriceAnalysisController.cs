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
}