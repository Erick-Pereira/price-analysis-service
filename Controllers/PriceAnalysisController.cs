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
            decimal marketPrice = 4000;

            var difference = ((request.PricePaid - marketPrice) / marketPrice) * 100;

            string status;

            if (difference > 50)
                status = "SUPERFATURADO";
            else if (difference > 20)
                status = "SUSPEITO";
            else
                status = "NORMAL";

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

    public class PriceRequest
    {
        public string Name { get; set; }
        public decimal PricePaid { get; set; }
    }
}