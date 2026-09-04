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
            var result =await _service.GetProductTypesAsync(urunGrupId);
            return Ok(result);
        }

        // CK ÜRÜNLERİ
        [HttpGet("ck-products")]
        public async Task<IActionResult> GetCKProducts([FromQuery] string? urunGrupId,[FromQuery] string? urunTipiId)
        {
            var result =await _service.GetCKProductDetailsAsync(urunGrupId,urunTipiId);
            return Ok(result);
        }

        // CK RENKLERİ

        [HttpGet("ck-colors")]
        public async Task<IActionResult> GetCKColors([FromQuery] string? katalogKodu,[FromQuery] string? ayakRal = null,[FromQuery] string? govdeKanatRal = null)
        {
            var result =await _service.GetCKProductDetailColorsAsync(katalogKodu,ayakRal,govdeKanatRal);
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
    }
}