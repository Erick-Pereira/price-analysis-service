using Microsoft.AspNetCore.Mvc;
using Simcag.PriceAnalysisService.Application.Interfaces;

namespace price_analysis_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PriceAnalysisController : ControllerBase
    {
        private readonly IPriceAnalysisService _service;
        private readonly IPriceRepository _repository;

        public PriceAnalysisController(IPriceAnalysisService service, IPriceRepository repository)
        {
            _service = service;
            _repository = repository;
        }

        // 🔥 POST - Analisa e salva
        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] PriceRequest request)
        {
            var result = _service.Analyze(request.Name, request.PricePaid);

            return Ok(result);
        }

        // 🔥 GET - Retorna histórico
        [HttpGet("history")]
        public IActionResult GetHistory()
        {
            var history = _repository.GetAll();

            return Ok(history);
        }
    }

    public class PriceRequest
    {
        public required string Name { get; set; }
        public required decimal PricePaid { get; set; }
    }
}