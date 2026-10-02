using GranitWebApi.Data;
using GranitWebApi.Services.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Notifications
{
    public class WorkflowNotificationService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public WorkflowNotificationService(
            AppDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
        public async Task SendWorkflowSubmittedAsync(int requestId)
        {
            var request = await _context.ProcessRequest
                .Include(x => x.Approvals)
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
            {
                Console.WriteLine("Request bulunamadı.");
                return;
            }


            var approverIds = request.Approvals
                .Where(x => x.IsActive)
                .Select(x => x.ApproverId)
                .Distinct()
                .ToList();


            Console.WriteLine($"Aktif onaycı sayısı: {approverIds.Count}");


            if (!approverIds.Any())
            {
                Console.WriteLine("Aktif onaycı yok.");
                return;
            }


            var processName = await _context.WorkflowDefinition
                .Where(x =>
                    x.ProcessTypeId == request.ProcessTypeId &&
                    x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();


            foreach (var approverId in approverIds)
            {

                Console.WriteLine($"Mail hazırlanıyor. UserId: {approverId}");


                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == approverId);


                if (user == null)
                {
                    Console.WriteLine($"User bulunamadı: {approverId}");
                    continue;
                }


                Console.WriteLine($"Email: {user.Email}");


                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine("Email boş.");
                    continue;
                }


                var notification = new NotificationModel
                {
                    To = user.Email,

                    Subject = $"Yeni Onay Bekleyen {processName ?? "Talep"}",

                    Template = "WorkflowSubmitted",

                    Values = new Dictionary<string, string>
                    {
                        ["TalepNo"] = request.Id.ToString(),
                        ["SurecAdi"] = processName ?? "-",
                        ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                        ["Durum"] = request.Status
                    }
                };


                Console.WriteLine("NotificationService çağrılıyor...");


                await _notificationService.SendAsync(notification);


                Console.WriteLine("Mail gönderme tamamlandı.");
            }
        }
        public async Task SendWorkflowReturnedAsync(
    int requestId,
    string? comment)
        {
            var request = await _context.ProcessRequest
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return;

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.CreatedBy);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var processName = await _context.WorkflowDefinition
                .Where(x =>
                    x.ProcessTypeId == request.ProcessTypeId &&
                    x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            var notification = new NotificationModel
            {
                To = user.Email,

                Subject = $"Revize Talebi - {processName ?? "Workflow"}",

                Template = "WorkflowReturned",

                Values = new Dictionary<string, string>
                {
                    ["TalepNo"] = request.Id.ToString(),
                    ["SurecAdi"] = processName ?? "-",
                    ["Durum"] = request.Status,
                    ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    ["RevizeNedeni"] = comment ?? "-"
                }
            };

            await _notificationService.SendAsync(notification);
        }
        public async Task SendWorkflowRejectedAsync(
    int requestId,
    string? comment)
        {
            var request = await _context.ProcessRequest
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return;

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.CreatedBy);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var processName = await _context.WorkflowDefinition
                .Where(x =>
                    x.ProcessTypeId == request.ProcessTypeId &&
                    x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            var notification = new NotificationModel
            {
                To = user.Email,

                Subject = $"Talebiniz Reddedildi - {processName ?? "Workflow"}",

                Template = "WorkflowRejected",

                Values = new Dictionary<string, string>
                {
                    ["TalepNo"] = request.Id.ToString(),
                    ["SurecAdi"] = processName ?? "-",
                    ["Durum"] = request.Status,
                    ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    ["RedNedeni"] = comment ?? "-"
                }
            };

            await _notificationService.SendAsync(notification);
        }
        public async Task SendWorkflowCompletedAsync(int requestId)
        {
            var request = await _context.ProcessRequest
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return;

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.CreatedBy);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var processName = await _context.WorkflowDefinition
                .Where(x =>
                    x.ProcessTypeId == request.ProcessTypeId &&
                    x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            var notification = new NotificationModel
            {
                To = user.Email,

                Subject = $"{processName ?? "Talep"} Tamamlandı",

                Template = "WorkflowCompleted",

                Values = new Dictionary<string, string>
                {
                    ["TalepNo"] = request.Id.ToString(),
                    ["SurecAdi"] = processName ?? "-",
                    ["Durum"] = request.Status,
                    ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                }
            };

            await _notificationService.SendAsync(notification);
        }
        public async Task SendPrepaymentRequiredAsync(int requestId)
        {
            var request = await _context.ProcessRequest
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return;

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.CreatedBy);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var processName = await _context.WorkflowDefinition
                .Where(x =>
                    x.ProcessTypeId == request.ProcessTypeId &&
                    x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            var notification = new NotificationModel
            {
                To = user.Email,

                Subject = $"{processName ?? "Proforma"} - Ön Ödeme Bilgisi Bekleniyor",

                Template = "WorkflowApproved",

                Values = new Dictionary<string, string>
                {
                    ["TalepNo"] = request.Id.ToString(),
                    ["SurecAdi"] = processName ?? "-",
                    ["Durum"] = "ON_ODEME_BEKLIYOR",
                    ["Tarih"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                }
            };

            await _notificationService.SendAsync(notification);
        }
    }
}