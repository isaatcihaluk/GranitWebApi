using GranitWebApi.Services.Sales.Definitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales.Definitions
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesDefinitionController : ControllerBase
    {
        private readonly ISalesDefinitionService _service;

        public SalesDefinitionController(ISalesDefinitionService service)
        {
            _service = service;
        }

        // ÜRÜN GRUPLARI
        [HttpGet("product-groups")]
        public async Task<IActionResult> GetProductGroups()
        {
            var result = await _service.GetProductGroupsAsync();
            return Ok(result);
        }

        // ÜRÜN TİPLERİ
        [HttpGet("product-types")]
        public async Task<IActionResult> GetProductTypes([FromQuery] string? urunGrupId)
        {
            var result = await _service.GetProductTypesAsync(urunGrupId);
            return Ok(result);
        }

        // CK ÜRÜNLERİ
        [HttpGet("ck-products")]
        public async Task<IActionResult> GetCKProducts([FromQuery] string? urunGrupId, [FromQuery] string? urunTipiId)
        {
            var result = await _service.GetCKProductDetailsAsync(urunGrupId, urunTipiId);
            return Ok(result);
        }

        // CK RENKLERİ

        [HttpGet("ck-colors")]
        public async Task<IActionResult> GetCKColors([FromQuery] string? katalogKodu, [FromQuery] string? ayakRal = null, [FromQuery] string? govdeKanatRal = null)
        {
            var result = await _service.GetCKProductDetailColorsAsync(katalogKodu, ayakRal, govdeKanatRal);
            return Ok(result);
        }

        //GÖRSEL TİPLERİ

        [HttpGet("sales-order-image-types")]
        public async Task<IActionResult> GetSalesOrderImageTypes()
        {
            var result = await _service.GetSalesOrderImageTypesAsync();
            return Ok(result);
        }

        // PLASTİK RENKLER
        [HttpGet("plastic-colors")]
        public async Task<IActionResult> GetPlasticColors()
        {
            var result =
                await _service.GetPlasticColorsAsync();

            return Ok(result);
        }

        [HttpGet("um/body-types")]
        public async Task<IActionResult> GetUMBodyTypes()
        {
            return Ok(await _service.GetUMBodyTypesAsync());
        }

        [HttpGet("um/bodies")]
        public async Task<IActionResult> GetUMBodies([FromQuery] string? bodyType)
        {
            return Ok(await _service.GetUMBodiesAsync(bodyType));
        }

        [HttpGet("um/ironing-boards")]
        public async Task<IActionResult> GetUMIroningBoards([FromQuery] string? bodyCode)
        {
            return Ok(await _service.GetUMIroningBoardsAsync(bodyCode));
        }

        [HttpGet("um/feet")]
        public async Task<IActionResult> GetUMFeet([FromQuery] string? bodyCode, [FromQuery] string? ironingBoardCode)
        {
            return Ok(await _service.GetUMFeetAsync(bodyCode, ironingBoardCode));
        }

        [HttpGet("um/product")]
        public async Task<IActionResult> GetUMProduct([FromQuery] string? bodyCode, [FromQuery] string? ironingBoardCode, [FromQuery] string? footCode)
        {
            var result = await _service.GetUMProductAsync(bodyCode, ironingBoardCode, footCode);
            if (result == null)
            {
                return NotFound(new
                {
                    message = "Seçilen UM kombinasyonuna ait ürün bulunamadı."
                });
            }
            return Ok(result);
        }

        [HttpGet("um/product-colors")]
        public async Task<IActionResult> GetUMProductColors([FromQuery] string? katalogKodu)
        {
            return Ok(await _service.GetUMProductColorsAsync(katalogKodu));
        }

        [HttpGet("um/fabrics")]
        public async Task<IActionResult> GetUMFabrics([FromQuery] string? katalogKodu)
        {
            return Ok(await _service.GetUMFabricsAsync(katalogKodu));
        }

        [HttpGet("um/sponges")]
        public async Task<IActionResult> GetUMSponges([FromQuery] string? katalogKodu)
        {
            return Ok(await _service.GetUMSpongesAsync(katalogKodu));
        }

        [HttpGet("um/fis-types")]
        public async Task<IActionResult> GetUMFisTypes()
        {
            return Ok(await _service.GetUMFisTypesAsync());
        }

        [HttpGet("um/fis-codes")]
        public async Task<IActionResult> GetUMFisCodes([FromQuery] string? fisTip)
        {
            return Ok(await _service.GetUMFisCodesAsync(fisTip));
        }

        [HttpGet("um/plastic-combinations")]
        public async Task<IActionResult> GetUMPlasticCombinations([FromQuery] string? kumasKod)
        {
            return Ok(await _service.GetUMPlasticCombinationsAsync(kumasKod));
        }

        [HttpGet("yk/types")]
        public async Task<IActionResult> GetYKTypes()
        {
            return Ok(await _service.GetYKTypesAsync());
        }

        [HttpGet("yk/fabrics")]
        public async Task<IActionResult> GetYKFabrics([FromQuery] string? katalogKodu)
        {
            return Ok(await _service.GetYKFabricsAsync(katalogKodu));
        }
    }
}