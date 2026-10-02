using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderLineUMConfigurationController : ControllerBase
    {
        private readonly ISalesOrderLineUMConfigurationService _configurationService;

        public SalesOrderLineUMConfigurationController(ISalesOrderLineUMConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        // GET: api/SalesOrderLineUMConfiguration/line/1
        [HttpGet("line/{salesOrderLineId:long}")]
        public async Task<IActionResult> GetBySalesOrderLineId(long salesOrderLineId)
        {
            try
            {
                var configurations =
                    await _configurationService.GetBySalesOrderLineIdAsync(salesOrderLineId);

                return Ok(configurations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/SalesOrderLineUMConfiguration/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var configuration = await _configurationService.GetByIdAsync(id);

                if (configuration == null)
                {
                    return NotFound(new
                    {
                        message = "UM konfigürasyonu bulunamadı."
                    });
                }

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/SalesOrderLineUMConfiguration
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SalesOrderLineUMConfiguration configuration)
        {
            try
            {
                var result = await _configurationService.CreateAsync(configuration);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/SalesOrderLineUMConfiguration/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] SalesOrderLineUMConfiguration configuration)
        {
            try
            {
                var result = await _configurationService.UpdateAsync(id, configuration);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "UM konfigürasyonu bulunamadı."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/SalesOrderLineUMConfiguration/1
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _configurationService.DeleteAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "UM konfigürasyonu bulunamadı."
                    });
                }

                return Ok(new
                {
                    message = "UM konfigürasyonu silindi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}