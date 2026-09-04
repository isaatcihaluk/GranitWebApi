using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;
        private readonly UserContext _userContext;
        private readonly AppDbContext _context;

        public SalesController(ISalesOrderService salesOrderService,UserContext userContext, AppDbContext context)
        {
            _salesOrderService = salesOrderService;
            _userContext = userContext;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesOrder salesOrder)
        {
            try
            {
                var result = await _salesOrderService.CreateAsync(salesOrder,_userContext.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _salesOrderService.GetAllAsync(_userContext.UserId);
            return Ok(data);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _salesOrderService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id,[FromBody] SalesOrder salesOrder)
        {
            try
            {
                var result = await _salesOrderService.UpdateAsync(id,salesOrder,_userContext.UserId);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new {message = ex.Message});
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
        {
            var customers = await _context.SalesCustomers.AsNoTracking()
                .OrderBy(x => x.Sirket)
                .ThenBy(x => x.FirmaKod)
                .ToListAsync(cancellationToken);

            return Ok(customers);
        }

        [HttpGet("{salesOrderId:long}/lines")]
        public async Task<IActionResult> GetLines(long salesOrderId)
        {
            try
            {
                var lines = await _salesOrderService.GetLinesAsync(salesOrderId);

                return Ok(lines);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{id}/check-erp")]
        public async Task<IActionResult> CheckErp(long id)
        {
            try
            {
                var result = await _salesOrderService.CheckErpAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:long}/send-for-approval")]
        public async Task<IActionResult> SendForApproval(long id)
        {
            try
            {
                var processRequestId =
                    await _salesOrderService.SendForApprovalAsync(
                        id,
                        _userContext.UserId);

                return Ok(new
                {
                    message = "Satış siparişi teknik onaya gönderildi.",
                    processRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{id:long}/complete-technical")]
        public async Task<IActionResult> CompleteTechnical(long id)
        {
            try
            {
                await _salesOrderService.CompleteTechnicalAsync(
                    id,
                    _userContext.UserId);

                return Ok(new
                {
                    message = "Teknik aşama tamamlandı. Sipariş paketleme aşamasına geçti."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}