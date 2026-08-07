using GranitWebApi.Services.Trendyol;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrendyolController : ControllerBase
    {
        private readonly ITrendyolService _trendyolService;

        public TrendyolController(ITrendyolService trendyolService)
        {
            _trendyolService = trendyolService;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var result = await _trendyolService.GetOrdersAsync();
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import()
        {
            await _trendyolService.ImportOrdersAsync();
            return Ok("Import işlemi tamamlandı");
        }
    }
}