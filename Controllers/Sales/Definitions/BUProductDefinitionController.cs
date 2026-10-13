using GranitWebApi.Services.Sales.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales.Definitions
{
    [ApiController]
    [Route("api/sales/definitions/bu")]
    public class BUProductDefinitionController : ControllerBase
    {
        private readonly BUProductDefinitionService _service;

        public BUProductDefinitionController(
            BUProductDefinitionService service)
        {
            _service = service;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var result =
                await _service.GetProductsAsync();

            return Ok(result);
        }

        [HttpGet("products/{katalogKod}/colors")]
        public async Task<IActionResult> GetColors(
            string katalogKod)
        {
            var result =
                await _service.GetColorsAsync(katalogKod);

            return Ok(result);
        }

        [HttpGet("etiket-akmaz")]
        public async Task<IActionResult> GetEtiketAkmaz()
        {
            var result =
                await _service.GetEtiketAkmazAsync();

            return Ok(result);
        }

        [HttpGet("products/{katalogKod}/kumas")]
        public async Task<IActionResult> GetKumas(
            string katalogKod)
        {
            var result =
                await _service.GetKumasAsync(katalogKod);

            return Ok(result);
        }
    }
}
