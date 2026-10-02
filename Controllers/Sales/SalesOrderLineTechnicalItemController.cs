using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderLineTechnicalItemController : ControllerBase
    {
        private readonly ISalesOrderLineTechnicalItemService _technicalItemService;

        public SalesOrderLineTechnicalItemController(ISalesOrderLineTechnicalItemService technicalItemService)
        {
            _technicalItemService = technicalItemService;
        }

        // GET: api/SalesOrderLineTechnicalItem/line/1
        [HttpGet("line/{salesOrderLineId:long}")]
        public async Task<IActionResult> GetBySalesOrderLineId(long salesOrderLineId)
        {
            try
            {
                var technicalItems = await _technicalItemService.GetBySalesOrderLineIdAsync(salesOrderLineId);
                return Ok(technicalItems);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // GET: api/SalesOrderLineTechnicalItem/image/1
        [HttpGet("image/{imageId:long}")]
        public async Task<IActionResult> GetByImageId(long imageId)
        {
            try
            {
                var technicalItems = await _technicalItemService.GetByImageIdAsync(imageId);
                return Ok(technicalItems);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // GET: api/SalesOrderLineTechnicalItem/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var technicalItem = await _technicalItemService.GetByIdAsync(id);

                if (technicalItem == null)
                {
                    return NotFound(new { message = "Teknik ürün kaydı bulunamadı." });
                }
                return Ok(technicalItem);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // POST: api/SalesOrderLineTechnicalItem
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesOrderLineTechnicalItem technicalItem)
        {
            try
            {
                var result = await _technicalItemService.CreateAsync(technicalItem);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // PUT: api/SalesOrderLineTechnicalItem/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id,[FromBody] SalesOrderLineTechnicalItem technicalItem)
        {
            try
            {
                var result = await _technicalItemService.UpdateAsync(id, technicalItem);
                if (result == null)
                {
                    return NotFound(new {message = "Teknik ürün kaydı bulunamadı."});
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // DELETE: api/SalesOrderLineTechnicalItem/1
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _technicalItemService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new {message = "Teknik ürün kaydı bulunamadı."});
                }
                return Ok(new {message = "Teknik ürün kaydı pasif duruma getirildi."});
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        [HttpGet("stock-items")]
        public async Task<IActionResult> SearchStockItems()
        {
            var result = await _technicalItemService.GetStockInfoAsync();
            return Ok(result);
        }

        [HttpPost("save/{salesOrderLineId}")]
        public async Task<IActionResult> Save(long salesOrderLineId,[FromBody] List<SalesOrderLineTechnicalItem> technicalItems)
        {
            try
            {
                if (salesOrderLineId <= 0){return BadRequest(new {message = "Geçersiz sipariş satırı."});}
                technicalItems ??= new List<SalesOrderLineTechnicalItem>();

                await _technicalItemService.SaveTechnicalItemsAsync(salesOrderLineId,technicalItems);
                return Ok(new {message = "Teknik stok seçimleri başarıyla kaydedildi."});
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }
    }
}
