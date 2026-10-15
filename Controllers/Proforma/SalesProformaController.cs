using DocumentFormat.OpenXml.InkML;
using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.NETSISMODELLER;
using GranitWebApi.Models.Proforma;
using GranitWebApi.Services.Notification;
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
        private readonly ISalesProformaPdfService _proformaPdfService;
        private readonly SalesProformaNotificationService _proformaNotificationService;

        public SalesProformaController(ISalesProformaService proformaService, UserContext userContext,
            IWorkflowService workflowService, ErpDbContext erpContext, AppDbContext context,
            ISalesProformaPdfService proformaPdfService, SalesProformaNotificationService proformaNotificationService)
        {
            _proformaService = proformaService;
            _userContext = userContext;
            _workflowService = workflowService;
            _erpContext = erpContext;
            _context = context;
            _proformaPdfService = proformaPdfService;
            _proformaNotificationService = proformaNotificationService;
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
                var result = await _proformaService.CreateAsync(proforma, _userContext.UserId);
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
        public async Task<IActionResult> Update(int id, [FromBody] SalesProformaModel proforma)
        {
            try
            {
                var result = await _proformaService.UpdateAsync(id, proforma, _userContext.UserId);

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
        public async Task<IActionResult> Submit(int id, [FromBody] string? comment)
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
                    Title = $"Satış Proforması - {proforma.SystemProformaNumber}",
                    EntityId = proforma.Id,
                    Status = WorkflowStatus.Taslak.ToString(),
                    IsVisibleToHR = false,
                    Parameters = JsonSerializer.Serialize(new { PaymentType = proforma.PaymentType, PaymentCondition = proforma.PaymentCondition })
                };

                var requestId = await _workflowService.StartAndSubmitAsync(request, comment ?? "Proforma onaya gönderildi.");
                var saved = await _proformaService.SetWorkflowAsync(id, requestId, "ONAY_BEKLIYOR", _userContext.UserId);
                if (!saved)
                {
                    return StatusCode(500, new
                    {
                        basarili = false,
                        message = "Workflow oluşturuldu ancak proforma kaydı güncellenemedi.",
                        processRequestId = requestId
                    });
                }

                // Proforma kaydı başarıyla güncellendikten sonra bildirim gönder.
                try
                {
                    await _proformaNotificationService.SendApprovalPendingAsync(requestId);
                }
                catch (Exception mailEx)
                {
                    // E-posta hatası, başarılı workflow gönderimini başarısız yapmasın.
                    Console.WriteLine($"Proforma onay bekleme e-postası gönderilemedi: {mailEx.Message}");
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
        public async Task<IActionResult> Return(int id, [FromBody] string? comment)
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
                    !new[] { "ONAY_BEKLIYOR", "FINANS_ONAY_BEKLIYOR", "ON_ODEME_BEKLIYOR" }
                        .Contains(proforma.Status))
                {
                    return BadRequest(new
                    {
                        message = "Yalnızca workflow süreci bulunan, onay bekleyen proformalar revizeye iade edilebilir."
                    });
                }

                var requestId = proforma.ProcessRequestId.Value;

                // Önce workflow iade işlemi tamamlanır.
                await _workflowService.ReturnAsync(
                    requestId,
                    _userContext.UserId,
                    comment);

                // İade başarılıysa talep sahibine revize e-postası gönderilir.
                try
                {
                    await _proformaNotificationService.SendRevisionRequestedAsync(
                        requestId,
                        comment);
                }
                catch (Exception mailEx)
                {
                    // E-posta hatası başarılı workflow iadesini geri almamalı.
                    Console.WriteLine(
                        $"Proforma revize e-postası gönderilemedi. " +
                        $"RequestId: {requestId}, Hata: {mailEx.Message}");
                }

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma revize için iade edildi.",
                    proformaId = id,
                    processRequestId = requestId
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
        public async Task<IActionResult> Resubmit(
            int id,
            [FromBody] string? comment)
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

                if (proforma.Status != "REVIZE" &&
                    proforma.Status != "ON_ODEME_BEKLIYOR")
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

                var requestId = proforma.ProcessRequestId.Value;

                // Revize edilen workflow adımını tekrar gönderimden önce al.
                var workflowRequest = await _context.ProcessRequest
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (workflowRequest == null)
                {
                    return BadRequest(new
                    {
                        message = "Workflow kaydı bulunamadı."
                    });
                }

                var revisionStep = workflowRequest.CurrentStep;

                var parameters = JsonSerializer.Serialize(new
                {
                    PaymentType = proforma.PaymentType,
                    PaymentCondition = proforma.PaymentCondition
                });

                // Workflow tekrar gönderilir.
                await _workflowService.ResubmitAsync(
                    requestId,
                    _userContext.UserId,
                    comment ?? "Revize sonrası tekrar onaya gönderildi.",
                    parameters);

                // E-posta hatası workflow işlemini başarısız yapmamalı.
                try
                {
                    if (revisionStep == 1)
                    {
                        // İlk onay adımı revize edildiyse aktif onaycılara bildir.
                        await _proformaNotificationService.SendApprovalPendingAsync(requestId);
                    }

                    // revisionStep == 2 ise finans revizesidir.
                    // Workflow bekletilir; finans bildirimi ödeme bilgileri
                    // SubmitPaymentForFinance ile gönderildiğinde yapılacaktır.
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine(
                        $"Proforma tekrar gönderim e-postası gönderilemedi. " +
                        $"ProformaId: {id}, RequestId: {requestId}, " +
                        $"Hata: {mailEx.Message}");
                }

                var updatedProforma = await _proformaService.GetByIdAsync(id);

                return Ok(new
                {
                    basarili = true,
                    message = "Revize proforma tekrar gönderildi.",
                    proformaId = id,
                    processRequestId = requestId,
                    durum = updatedProforma?.Status
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
        public async Task<IActionResult> Cancel(int id, [FromBody] string? comment)
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

                var allowedStatuses = new[]
                {
            "TASLAK",
            "ONAY_BEKLIYOR",
            "REVIZE",
            "ON_ODEME_BEKLIYOR"
        };

                if (!allowedStatuses.Contains(proforma.Status))
                {
                    return BadRequest(new
                    {
                        message = "Bu durumdaki proforma iptal edilemez."
                    });
                }

                var requestId = proforma.ProcessRequestId;

                if (requestId.HasValue)
                {
                    await _workflowService.CancelAsync(
                        requestId.Value,
                        _userContext.UserId,
                        comment ?? "Proforma iptal edildi.");
                }

                var saved = await _proformaService.UpdateStatusAsync(
                    id,
                    "IPTAL",
                    _userContext.UserId);

                if (!saved)
                {
                    return StatusCode(500, new
                    {
                        basarili = false,
                        message = "Workflow iptal edildi ancak proforma durumu güncellenemedi."
                    });
                }

                // İptal ve durum güncellemesi başarılıysa e-posta gönder.
                if (requestId.HasValue)
                {
                    try
                    {
                        await _proformaNotificationService.SendCancelledAsync(
                            requestId.Value,
                            comment);
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine(
                            $"Proforma iptal e-postası gönderilemedi. " +
                            $"RequestId: {requestId.Value}, Hata: {mailEx.Message}");
                    }
                }

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma iptal edildi.",
                    proformaId = id,
                    processRequestId = requestId
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
        public async Task<IActionResult> Approve(int id, [FromBody] string? comment)
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

                var requestId = proforma.ProcessRequestId.Value;
                await _workflowService.ApproveAsync(requestId, _userContext.UserId, comment);
                var updatedProforma = await _proformaService.GetByIdAsync(id);
                if (updatedProforma?.Status == "TAMAMLANDI")
                {
                    try
                    {
                        await _proformaNotificationService.SendApprovedAsync(requestId);
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"Proforma onay e-postası gönderilemedi. " +
                            $"RequestId: {requestId}, Hata: {mailEx.Message}");
                    }
                }
                else
                {
                    try
                    {
                        await _proformaNotificationService.SendApprovalPendingAsync(requestId);
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"Sonraki onay adımı e-postası gönderilemedi. " +
                            $"RequestId: {requestId}, Hata: {mailEx.Message}");
                    }
                }

                return Ok(new
                {
                    basarili = true,
                    message = "Proforma onay işlemi gerçekleştirildi.",
                    proformaId = id,
                    processRequestId = requestId,
                    durum = updatedProforma?.Status
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
        public async Task<IActionResult> CreatePayment(int id, [FromBody] SalesProformaPaymentRequest request)
        {
            try
            {
                var userId = _userContext.UserId;
                var result = await _proformaService.CreatePaymentAsync(id, request, userId);

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

                await _proformaService.SubmitPaymentForFinanceAsync(id, userId);
                await _workflowService.ActivateStepAsync(proforma.ProcessRequestId.Value, 2, userId, "Ödeme bilgileri girildi, finans onayına gönderildi.");
                await _proformaNotificationService.SendApprovalPendingAsync(proforma.ProcessRequestId.Value,template: "FinanceApprovalPending");
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
                .Where(x => x.RequestId == processRequest.Id && x.ActionType == 3)
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
                var result = await _proformaService.ConvertToOrderAsync(id, _userContext.UserId);
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
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    innerException2 = ex.InnerException?.InnerException?.Message
                });
            }
        }

        #endregion

        #region Doc

        [HttpPost("{id}/document")]
        public async Task<IActionResult> UploadDocument(
            long id,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Proforma dosyası seçilmelidir."
                });
            }

            var result = await _proformaService.UploadDocumentAsync(
                id,
                file,
                _userContext.UserId);

            if (!result.Basarili)
            {
                return BadRequest(new
                {
                    message = result.Aciklama
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}/document")]
        public async Task<IActionResult> GetDocument(long id)
        {
            var result = await _proformaService.GetDocumentFileAsync(id);

            if (!result.Basarili || result.FileBytes == null)
            {
                return NotFound(new
                {
                    message = result.Aciklama
                });
            }

            return File(
                result.FileBytes,
                result.ContentType ?? "application/pdf",
                result.FileName,
                enableRangeProcessing: true);
        }

        #endregion

        [HttpPost("{id:int}/generate-pdf")]
        public async Task<IActionResult> GeneratePdf(int id)
        {
            try
            {
                var result = await _proformaPdfService.GenerateAndSaveAsync(
                    id,
                    _userContext.UserId);

                if (!result.Basarili)
                {
                    return BadRequest(new
                    {
                        message = result.Aciklama
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
    }
}