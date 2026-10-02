using DocumentFormat.OpenXml.InkML;
using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models;
using GranitWebApi.Models.NETSISMODELLER;
using GranitWebApi.Models.Proforma;
using GranitWebApi.Services.Proforma;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using SalesProformaModel = GranitWebApi.Models.Proforma.SalesProforma;

namespace GranitWebApi.Controllers.Proforma
{
    [ApiController]
    [Route("api/sales/proforma")]
    [Authorize]
    public class SalesProformaController : ControllerBase
    {
        private readonly ISalesProformaService _proformaService;
        private readonly UserContext _userContext;
        private readonly IWorkflowService _workflowService;
        private readonly ErpDbContext _erpContext;
        private readonly AppDbContext _context;

        public SalesProformaController(ISalesProformaService proformaService,UserContext userContext,
            IWorkflowService workflowService,ErpDbContext erpContext,AppDbContext context)
        {
            _proformaService = proformaService;
            _userContext = userContext;
            _workflowService = workflowService;
            _erpContext = erpContext;
            _context = context;
        }

        #region Get List
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            try
            {
                return Ok(await _proformaService.GetListAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Get By Id

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _proformaService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
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

        #endregion

        #region Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesProformaModel proforma)
        {
            try
            {
                var result = await _proformaService.CreateAsync(proforma,_userContext.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    innerException = ex.InnerException?.InnerException?.Message
                        ?? ex.InnerException?.Message
                });
            }
        }
        #endregion

        #region Update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id,[FromBody] SalesProformaModel proforma)
        {
            try
            {
                var result = await _proformaService.UpdateAsync(id,proforma,_userContext.UserId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
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
        #endregion

        #region Workflow - Submit
        [HttpPost("workflow/{id:int}/submit")]
        public async Task<IActionResult> Submit(int id,[FromBody] string? comment)
        {
            try
            {
                var proforma = await _proformaService.GetByIdAsync(id);
                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                if (proforma.Status != "TASLAK")
                {
                    return BadRequest(new
                    {
                        message =
                            "İlk gönderim yalnızca taslak durumundaki proformalar için yapılabilir. " +
                            "Revize proformalar için tekrar gönderim işlemini kullanın."
                    });
                }
                if (!proforma.Lines.Any(x => !x.DeletedFlag))
                {
                    return BadRequest(new
                    {
                        message = "Proformada en az bir ürün bulunmalıdır."
                    });
                }

                var processTypeId = await _proformaService.GetProcessTypeIdAsync();
                var request = new ProcessRequest
                {
                    ProcessTypeId = processTypeId,
                    CreatedBy = _userContext.UserId,
                    Title = $"Satış Proforması - {proforma.ProformaNumber}",
                    EntityId = proforma.Id,
                    Status = WorkflowStatus.Taslak.ToString(),
                    IsVisibleToHR = false,
                    Parameters = JsonSerializer.Serialize(new {PaymentType = proforma.PaymentType})
                };

                var requestId = await _workflowService.StartAndSubmitAsync(request,comment ?? "Proforma onaya gönderildi.");

                var saved = await _proformaService.SetWorkflowAsync(id,requestId,"ONAY_BEKLIYOR",_userContext.UserId);
                if (!saved)
                {
                    return StatusCode(500, new
                    {
                        basarili = false,
                        message = "Workflow oluşturuldu ancak proforma kaydı güncellenemedi.",
                        processRequestId = requestId
                    });
                }

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma onaya gönderildi.",
                    processRequestId = requestId,
                    proformaId = id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Workflow - Return
        [HttpPost("workflow/{id:int}/return")]
        public async Task<IActionResult> Return(int id,[FromBody] string? comment)
        {
            try
            {
                var proforma = await _proformaService.GetByIdAsync(id);
                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                if (proforma.ProcessRequestId == null || 
                    !new[] {"ONAY_BEKLIYOR","FINANS_ONAY_BEKLIYOR","ON_ODEME_BEKLIYOR"}.Contains(proforma.Status))
                {
                    return BadRequest(new
                    {
                        message = "Yalnızca workflow süreci bulunan, onay bekleyen proformalar revizeye iade edilebilir."
                    });
                }

                await _workflowService.ReturnAsync(proforma.ProcessRequestId.Value,_userContext.UserId,comment);

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma revize için iade edildi.",
                    proformaId = id,
                    processRequestId = proforma.ProcessRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }

        #endregion

        #region Workflow - Resubmit
        [HttpPost("workflow/{id:int}/resubmit")]
        public async Task<IActionResult> Resubmit(int id,[FromBody] string? comment)
        {
            try
            {
                var proforma = await _proformaService.GetByIdAsync(id);

                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                if (proforma.Status != "REVIZE" && proforma.Status != "ON_ODEME_BEKLIYOR")
                {
                    return BadRequest(new
                    {
                        message = "Bu durumdaki proforma tekrar gönderilemez."
                    });
                }
                if (proforma.ProcessRequestId == null)
                {
                    return BadRequest(new
                    {
                        message = "Proformaya ait mevcut workflow süreci bulunamadı. " +
                            "İlk gönderim işlemini kontrol edin."
                    });
                }
                if (!proforma.Lines.Any(x => !x.DeletedFlag))
                {
                    return BadRequest(new
                    {
                        message = "Proformada en az bir ürün bulunmalıdır."
                    });
                }

                var parameters = JsonSerializer.Serialize(new {PaymentType = proforma.PaymentType});

                await _workflowService.ResubmitAsync(proforma.ProcessRequestId.Value,_userContext.UserId,
                    comment ?? "Revize sonrası tekrar onaya gönderildi.",parameters);

                return Ok(new
                {
                    basarili = true,
                    message = "Revize proforma tekrar gönderildi.",
                    proformaId = id,
                    processRequestId = proforma.ProcessRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Workflow - Cancel
        [HttpPost("workflow/{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id,[FromBody] string? comment)
        {
            try
            {
                var proforma = await _proformaService.GetByIdAsync(id);
                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }

                var allowedStatuses = new[] {"TASLAK","ONAY_BEKLIYOR","REVIZE","ON_ODEME_BEKLIYOR"};
                if (!allowedStatuses.Contains(proforma.Status))
                {
                    return BadRequest(new
                    {
                        message = "Bu durumdaki proforma iptal edilemez."
                    });
                }

                if (proforma.ProcessRequestId.HasValue)
                {
                    await _workflowService.CancelAsync(proforma.ProcessRequestId.Value,_userContext.UserId,comment ?? "Proforma iptal edildi.");
                }

                var saved = await _proformaService.UpdateStatusAsync(id,"IPTAL",_userContext.UserId);

                if (!saved)
                {
                    return StatusCode(500, new
                    {
                        basarili = false,
                        message = "Workflow iptal edildi ancak proforma durumu güncellenemedi."
                    });
                }

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma iptal edildi.",
                    proformaId = id,
                    processRequestId = proforma.ProcessRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Workflow - Approve
        [HttpPost("workflow/{id:int}/approve")]
        public async Task<IActionResult> Approve(int id,[FromBody] string? comment)
        {
            try
            {
                var proforma = await _proformaService.GetByIdAsync(id);
                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                if (proforma.ProcessRequestId == null)
                {
                    return BadRequest(new
                    {
                        message = "Proformaya ait workflow süreci bulunamadı."
                    });
                }

                await _workflowService.ApproveAsync(proforma.ProcessRequestId.Value,_userContext.UserId,comment);

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma onay işlemi gerçekleştirildi.",
                    proformaId = id,
                    processRequestId = proforma.ProcessRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Payment
        [HttpPost("{id}/payment")]
        public async Task<IActionResult> CreatePayment(int id,[FromBody] SalesProformaPaymentRequest request)
        {
            try
            {
                var userId = _userContext.UserId;
                var result = await _proformaService.CreatePaymentAsync(id,request,userId);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }

                return Ok(new
                {
                    message = "Ödeme bilgisi kaydedildi.",
                    proformaId = id
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
        #endregion

        #region Payment - Submit For Finance
        [HttpPost("{id}/payment/submit")]
        public async Task<IActionResult> SubmitPaymentForFinance(int id)
        {
            try
            {
                var userId = _userContext.UserId;
                var proforma = await _proformaService.GetByIdAsync(id);
                if (proforma == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                if (proforma.Status != "ON_ODEME_BEKLIYOR")
                {
                    return BadRequest(new
                    {
                        message = "Proforma ödeme bekleme durumunda değil."
                    });
                }
                if (proforma.ProcessRequestId == null)
                {
                    return BadRequest(new
                    {
                        message = "Proformaya bağlı workflow bulunamadı."
                    });
                }
                if (proforma.CreatedBy != userId)
                {
                    return BadRequest(new
                    {
                        message = "Ödeme bilgilerini sadece proforma oluşturan kişi finans onayına gönderebilir."
                    });
                }

                await _proformaService.SubmitPaymentForFinanceAsync(id,userId);
                await _workflowService.ActivateStepAsync(proforma.ProcessRequestId.Value,2,userId,"Ödeme bilgileri girildi, finans onayına gönderildi.");

                return Ok(new
                {
                    message = "Ödeme bilgileri finans onayına gönderildi.",
                    proformaId = id,
                    processRequestId = proforma.ProcessRequestId
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
        #endregion

        #region Product Groups
        [HttpGet("product-groups")]
        public async Task<IActionResult> GetProductGroups()
        {
            var productGroups =
                await _erpContext.Database
                    .SqlQueryRaw<ProductGroupResult>(
                        @"
                        SELECT 
                            GRUP_KOD AS GrupKod, 
                            GRUP_ISIM AS GrupIsim 
                        FROM TBLSTOKKOD2 
                        WHERE GRUP_KOD LIKE '1%' 
                        ORDER BY GRUP_KOD
                        ")
                    .ToListAsync();

            return Ok(productGroups);
        }
        #endregion

        [HttpGet("{id}/workflow-history")]
        public async Task<IActionResult> GetWorkflowHistory(int id)
        {
            var processRequest = await _context.ProcessRequest
                .AsNoTracking().FirstOrDefaultAsync(x => x.EntityId == id && x.ProcessTypeId == 4);

            if (processRequest == null) return NotFound("Proforma için workflow kaydı bulunamadı.");

            var history = await _context.WorkflowHistory.AsNoTracking()
                .Where(x => x.RequestId == processRequest.Id && x.ActionType==3)
                .OrderBy(x => x.ActionDate)
                .Select(x => new
                {
                    x.Id,
                    x.RequestId,
                    x.UserId,
                    UserName = _context.Users.Where(u => u.Id == x.UserId)
                        .Select(u => !string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)
                            ? (u.FirstName + " " + u.LastName).Trim() : u.Username).FirstOrDefault(),
                    x.ActionType,
                    Action = ((WorkflowAction)x.ActionType).ToString(),
                    x.ActionDate,
                    x.Comment
                }).ToListAsync();
            return Ok(history);
        }

        #region Convert To Order
        [HttpPost("{id:int}/convert-to-order")]
        public async Task<IActionResult> ConvertToOrder(int id)
        {
            try
            {
                var result = await _proformaService.ConvertToOrderAsync(id,_userContext.UserId);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Proforma bulunamadı."
                    });
                }
                return Ok(new
                {
                    basarili = true,
                    message = "Proforma siparişe dönüştürüldü.",
                    orderId = result.Id,
                    orderNumber = result.OrderNumber,
                    systemOrderNumber = result.SystemOrderNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    basarili = false,
                    message = ex.Message
                });
            }
        }

        #endregion

    }
}