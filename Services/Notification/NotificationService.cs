
using GranitWebApi.Data;
using GranitWebApi.Enums;
using GranitWebApi.Models;
using GranitWebApi.Services.Email;
using GranitWebApi.Services.Notifications.Models;

namespace GranitWebApi.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailTemplateService _templateService;

        public NotificationService(
            AppDbContext context,
            IEmailTemplateService templateService)
        {
            _context = context;
            _templateService = templateService;
        }

        public async Task SendAsync(NotificationModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var recipients = new[] { model.To }
                .Concat(model.ToEmails ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
            {
                throw new ArgumentException(
                    "En az bir e-posta alıcısı belirtilmelidir.",
                    nameof(model));
            }

            var html = await _templateService.RenderAsync(
                model.Template,
                model.Values);

            var queue = new EmailQueue
            {
                ToEmail = string.Join(";", recipients),
                CcEmails = JoinEmails(model.Cc),
                BccEmails = JoinEmails(model.Bcc),
                Subject = model.Subject,
                Template = model.Template,
                Body = html,
                Status = EmailQueueStatus.Pending,
                CreatedDate = DateTime.Now,

                Attachments = (model.Attachments
                    ?? new List<Services.Email.Models.EmailAttachmentModel>())
                    .Select(x => new EmailQueueAttachment
                    {
                        FileName = x.FileName,
                        ContentType = string.IsNullOrWhiteSpace(x.ContentType)
                            ? "application/octet-stream"
                            : x.ContentType,
                        Content = x.Content
                    })
                    .ToList()
            };

            _context.EmailQueue.Add(queue);

            // Queue ve ekleri aynı SaveChanges çağrısında kaydedilir.
            await _context.SaveChangesAsync();
        }

        private static string? JoinEmails(IEnumerable<string>? emails)
        {
            var result = emails?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result is { Count: > 0 }
                ? string.Join(";", result)
                : null;
        }
    }
}
