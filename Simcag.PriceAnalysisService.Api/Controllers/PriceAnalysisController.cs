using Microsoft.AspNetCore.Mvc;

namespace price_analysis_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PriceAnalysisController : ControllerBase
    {
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
}