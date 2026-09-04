using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderAssemblyCodeRequestController : ControllerBase
    {
        private readonly ISalesOrderAssemblyCodeRequestService _assemblyCodeRequestService;

        public SalesOrderAssemblyCodeRequestController(ISalesOrderAssemblyCodeRequestService assemblyCodeRequestService)
        {
            _assemblyCodeRequestService = assemblyCodeRequestService;
        }

        // GET: api/SalesOrderAssemblyCodeRequest/line/1
        [HttpGet("line/{salesOrderLineId:long}")]
        public async Task<IActionResult> GetBySalesOrderLineId(long salesOrderLineId)
        {
            try
            {
                var requests = await _assemblyCodeRequestService.GetBySalesOrderLineIdAsync(salesOrderLineId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // GET: api/SalesOrderAssemblyCodeRequest/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var request = await _assemblyCodeRequestService.GetByIdAsync(id);

                if (request == null)
                {
                    return NotFound(new
                    {
                        message = "Assembly code talep kaydı bulunamadı."
                    });
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // POST: api/SalesOrderAssemblyCodeRequest
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesOrderAssemblyCodeRequest request)
        {
            try
            {
                var result = await _assemblyCodeRequestService.CreateAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/SalesOrderAssemblyCodeRequest/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id,[FromBody] SalesOrderAssemblyCodeRequest request)
        {
            try
            {
                var result = await _assemblyCodeRequestService.UpdateAsync(id, request);
                if (result == null)
                {
                    return NotFound(new {message = "Assembly code talep kaydı bulunamadı."});
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/SalesOrderAssemblyCodeRequest/1
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _assemblyCodeRequestService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Assembly code talep kaydı bulunamadı."
                    });
                }

                return Ok(new
                {
                    message = "Assembly code talep kaydı silindi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }
    }
}
