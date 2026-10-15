using GranitWebApi.Data;
using GranitWebApi.Models.Proforma;
using GranitWebApi.Services.Email.Models;
using GranitWebApi.Services.Notifications;
using GranitWebApi.Services.Notifications.Models;
using GranitWebApi.Services.Proforma;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Notification
{
    public class SalesProformaNotificationService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ISalesProformaPdfService _proformaPdfService;

        public SalesProformaNotificationService(AppDbContext context, INotificationService notificationService,
            ISalesProformaPdfService proformaPdfService)
        {
            _context = context;
            _notificationService = notificationService;
            _proformaPdfService = proformaPdfService;
        }

        private async Task<ProcessRequest?> GetRequestAsync(int requestId)
        {
            return await _context.ProcessRequest.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == requestId);
        }

        private async Task<SalesProforma?> GetProformaByRequestAsync(int requestId)
        {
            return await _context.SalesProformas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProcessRequestId == requestId);
        }

        private async Task<string?> GetUserEmailAsync(int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0) return null;
            return await _context.Users.AsNoTracking()
                .Where(x => x.Id == userId.Value).Select(x => x.Email)
                .FirstOrDefaultAsync();
        }

        private async Task<List<string>> GetRepresentativeCcAsync(SalesProforma proforma, string? toEmail = null)
        {
            var emails = new List<string>();
            var representativeEmail = await GetUserEmailAsync(proforma.SalesRepresentativeUserId);
            if (string.IsNullOrWhiteSpace(representativeEmail)) return emails;

            // Alıcı ile müşteri temsilcisi aynı kişiyse CC'ye ekleme.
            if (!string.IsNullOrWhiteSpace(toEmail) &&
                string.Equals(toEmail, representativeEmail, StringComparison.OrdinalIgnoreCase))
            {
                return emails;
            }

            emails.Add(representativeEmail);
            return emails;
        }

        private async Task<EmailAttachmentModel> CreateProformaPdfAttachmentAsync(SalesProforma proforma)
        {
            var pdfBytes = await _proformaPdfService.GeneratePdfAsync(proforma.Id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Proforma PDF oluşturulamadı. ProformaId: {proforma.Id}");
            }

            var fileName = string.IsNullOrWhiteSpace(proforma.SystemProformaNumber)
                ? $"Proforma-{proforma.Id}.pdf" : $"{proforma.SystemProformaNumber}.pdf";

            return new EmailAttachmentModel
            {
                FileName = fileName,
                Content = pdfBytes,
                ContentType = "application/pdf"
            };
        }

        private async Task SendToCreatorAsync(ProcessRequest request,string subject,string template,Dictionary<string, string> values)
        {
            var proforma = await GetProformaByRequestAsync(request.Id);
            if (proforma == null) return;

            // To: Proformayı oluşturan kullanıcı
            var creatorEmail = await GetUserEmailAsync(proforma.CreatedBy);
            if (string.IsNullOrWhiteSpace(creatorEmail)) return;

            // CC: Müşteri temsilcisi
            var ccEmails = await GetRepresentativeCcAsync(proforma,creatorEmail);
            await _notificationService.SendAsync(new NotificationModel
            {
                To = creatorEmail,
                Cc = ccEmails,
                Subject = subject,
                Template = template,
                Values = values
            });
        }

        private Dictionary<string, string> CreateValues(ProcessRequest request,string status,string? comment = null)
        {
            var values = new Dictionary<string, string>
            {
                ["TalepNo"] = request.Id.ToString(),
                ["SurecAdi"] = request.Title ?? "Satış Proforması",
                ["Durum"] = status,
                ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };

            if (comment != null) values["Aciklama"] = comment;
            return values;
        }

        /// <summary>
        /// Aktif onaylayanlara bildirim gönderir.
        /// İlk onaydan sonraki bildirimde attachProformaPdf=true
        /// verilirse proforma PDF'si e-postaya eklenir.
        /// </summary>
        public async Task SendApprovalPendingAsync(int requestId,bool attachProformaPdf = false,string template = "WorkflowSubmitted")
        {
            var request = await _context.ProcessRequest
                .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null) return;
            var proforma = await GetProformaByRequestAsync(requestId);
            if (proforma == null) return;

            var approverIds = request.Approvals.Where(x => x.IsActive)
                .Select(x => x.ApproverId).Distinct().ToList();

            var values = CreateValues(request, request.Status);

            // PDF yalnızca bu metoda true gönderildiğinde oluşturulur.
            EmailAttachmentModel? pdfAttachment = null;
            if (attachProformaPdf)
            {
                pdfAttachment = await CreateProformaPdfAttachmentAsync(proforma);
            }

            foreach (var approverId in approverIds)
            {
                var approverEmail = await GetUserEmailAsync(approverId);
                if (string.IsNullOrWhiteSpace(approverEmail)) continue;

                var ccEmails = await GetRepresentativeCcAsync(proforma,approverEmail);
                var notification = new NotificationModel
                {
                    To = approverEmail,
                    Cc = ccEmails,
                    Subject =
                    $"Onay Bekliyor - {request.Title ?? "Satış Proforması"}",
                    Template = template,
                    Values = values
                };

                if (pdfAttachment != null)
                {
                    notification.Attachments = new List<EmailAttachmentModel>{pdfAttachment};
                }
                await _notificationService.SendAsync(notification);
            }
        }

        /// <summary>
        /// Son onayda TAMAMLANDI durumuna gelen proformanın
        /// PDF'sini oluşturarak tamamlanma e-postasına ekler.
        /// </summary>
        public async Task SendApprovedAsync(int requestId)
        {
            var request = await GetRequestAsync(requestId);
            if (request == null) return;

            var proforma = await GetProformaByRequestAsync(requestId);
            if (proforma == null) return;

            var creatorEmail = await GetUserEmailAsync(proforma.CreatedBy);
            if (string.IsNullOrWhiteSpace(creatorEmail)) return;

            var ccEmails = await GetRepresentativeCcAsync(proforma,creatorEmail);
            // Son onaydan sonra güncel PDF oluşturulur.
            var pdfAttachment = await CreateProformaPdfAttachmentAsync(proforma);

            var notification = new NotificationModel
            {
                To = creatorEmail,
                Cc = ccEmails,
                Subject = $"Proforma Onaylandı - {request.Title ?? "Satış Proforması"}",
                Template = "WorkflowCompleted",
                Values = CreateValues(request, "TAMAMLANDI"),
                Attachments = new List<EmailAttachmentModel>
                {
                    pdfAttachment
                }
            };
            await _notificationService.SendAsync(notification);
        }

        /// <summary>
        /// Revize bildirimi: oluşturan kullanıcı To, temsilci CC.
        /// </summary>
        public async Task SendRevisionRequestedAsync(int requestId,string? comment)
        {
            var request = await GetRequestAsync(requestId);
            if (request == null) return;

            var values = CreateValues(request, request.Status, comment);
            values["RevizeNedeni"] = comment ?? "-";

            await SendToCreatorAsync(request,
                $"Proforma Revize Edildi - {request.Title ?? "Satış Proforması"}",
                "WorkflowReturned",values);
        }

        /// <summary>
        /// İptal bildirimi: oluşturan kullanıcı To, temsilci CC.
        /// </summary>
        public async Task SendCancelledAsync(int requestId,string? comment)
        {
            var request = await GetRequestAsync(requestId);
            if (request == null) return;
            var values = CreateValues(request, "IPTAL", comment);
            values["IptalNedeni"] = comment ?? "-";
            await SendToCreatorAsync(request,$"Proforma İptal Edildi - {request.Title ?? "Satış Proforması"}",
                "WorkflowRejected",values);
        }

        public async Task SendPrepaymentRequiredAsync(int requestId)
        {
            var request = await GetRequestAsync(requestId);
            if (request == null) return;

            var proforma = await GetProformaByRequestAsync(requestId);
            if (proforma == null) return;

            var creatorEmail = await GetUserEmailAsync(proforma.CreatedBy);
            if (string.IsNullOrWhiteSpace(creatorEmail)) return;

            var ccEmails = await GetRepresentativeCcAsync(proforma, creatorEmail);
            var pdfAttachment = await CreateProformaPdfAttachmentAsync(proforma);

            await _notificationService.SendAsync(new NotificationModel
            {
                To = creatorEmail,
                Cc = ccEmails,
                Subject = $"{request.Title ?? "Satış Proforması"} - Ön Ödeme Bilgisi Bekleniyor",
                Template = "PrepaymentRequired",
                Values = CreateValues(request, "ON_ODEME_BEKLIYOR"),
                Attachments = new List<EmailAttachmentModel>{pdfAttachment}
            });
        }

    }
}