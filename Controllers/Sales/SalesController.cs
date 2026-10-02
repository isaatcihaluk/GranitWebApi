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
        private readonly ISalesOrderFormService _salesOrderFormService;
        private readonly UserContext _userContext;
        private readonly AppDbContext _context;

        public SalesController(ISalesOrderService salesOrderService, ISalesOrderFormService salesOrderFormService,UserContext userContext, AppDbContext context)
        {
            _salesOrderService = salesOrderService;
            _salesOrderFormService = salesOrderFormService;
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
                var processRequestId = await _salesOrderService.SendForApprovalAsync(id,_userContext.UserId);
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

        [HttpGet("{salesOrderId}/order-form")]
        public async Task<IActionResult> GetOrderForm(long salesOrderId)
        {
            try
            {
                var result = await _salesOrderFormService.GetOrderFormDataAsync(salesOrderId);
                if (result == null) return NotFound("Sipariş bulunamadı.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{salesOrderId}/order-form/pdf")]
        public async Task<IActionResult> GenerateOrderFormPdf(long salesOrderId)
        {
            try
            {
                var pdf = await _salesOrderFormService
                    .GenerateOrderFormPdfAsync(salesOrderId);

                return File(
                    pdf,
                    "application/pdf",
                    $"SiparisFormu-{salesOrderId}.pdf"
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/excel")]
        public async Task<IActionResult> GetOrderExcel(long id,[FromServices] SalesOrderExcelService excelService)
        {
            try
            {
                var file = await excelService.CreateSalesOrderExcelAsync(id);

                return File(
                    file,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SiparisFormu_{id}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        //SHRINK REÇETE OLUŞTURMA
        [HttpPost("{id:long}/prepare-shrink-recipes")]
        public async Task<IActionResult> PrepareShrinkRecipes(long id)
        {
            try
            {
                var result = await _salesOrderService.PrepareShrinkRecipesAsync(id);
                if (!result.Basarili) {return BadRequest(result);}
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    hata = true,
                    message = ex.Message
                });
            }
        }

        //PAKET REÇETE OLUŞTURMA
        [HttpPost("{id:long}/prepare-package-recipes")]
        public async Task<IActionResult> PreparePackageRecipes(long id)
        {
            try
            {
                var result = await _salesOrderService.PreparePackageRecipesAsync(id);
                if (!result.Basarili) { return BadRequest(result); }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    hata = true,
                    message = ex.Message
                });
            }
        }

        [HttpPost("{id:long}/transfer-netsis")]
        public async Task<IActionResult> TransferToNetsis(long id)
        {
            try
            {
                var result = await _salesOrderService.TransferToNetsisAsync(id);

                if (!result.Basarili)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    hata = true,
                    salesOrderId = id,
                    message = ex.Message
                });
            }
        }

        // YENİ: NETSIS AKTARILMIŞ SİPARİŞ ÜST BİLGİ REVİZYONU
        [HttpPut("{id:long}/revision-header")]
        public async Task<IActionResult> UpdateRevisionHeader(long id,[FromBody] SalesOrder salesOrder)
        {
            try
            {
                var result = await _salesOrderService.UpdateRevisionHeaderAsync(id,salesOrder,_userContext.UserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
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