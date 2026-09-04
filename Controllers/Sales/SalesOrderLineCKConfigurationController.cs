using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderLineCKConfigurationController : ControllerBase
    {
        private readonly ISalesOrderLineCKConfigurationService _configurationService;

        public SalesOrderLineCKConfigurationController(ISalesOrderLineCKConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        // GET: api/SalesOrderLineCKConfiguration/line/1
        [HttpGet("line/{salesOrderLineId:long}")]
        public async Task<IActionResult> GetBySalesOrderLineId(long salesOrderLineId)
        {
            try
            {
                var configurations =await _configurationService.GetBySalesOrderLineIdAsync(salesOrderLineId);
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/SalesOrderLineCKConfiguration/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var configuration =await _configurationService.GetByIdAsync(id);
                if (configuration == null) { return NotFound(new { message = "CK konfigürasyonu bulunamadı." });}
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // POST: api/SalesOrderLineCKConfiguration
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesOrderLineCKConfiguration configuration)
        {
            try
            {
                var result =await _configurationService.CreateAsync(configuration);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/SalesOrderLineCKConfiguration/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id,[FromBody] SalesOrderLineCKConfiguration configuration)
        {
            try
            {
                var result =await _configurationService.UpdateAsync(id, configuration);
                if (result == null) { return NotFound(new {message = "CK konfigürasyonu bulunamadı."});}
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        // DELETE: api/SalesOrderLineCKConfiguration/1
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _configurationService.DeleteAsync(id);
                if (!result) { return NotFound(new { message = "CK konfigürasyonu bulunamadı."});}
                return Ok(new { message = "CK konfigürasyonu silindi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }
    }
}
