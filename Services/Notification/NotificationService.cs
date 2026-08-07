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
            var html = await _templateService.RenderAsync(
                model.Template,
                model.Values);

            var queue = new EmailQueue
            {
                ToEmail = model.To,
                Subject = model.Subject,
                Template = model.Template,
                Body = html,
                Status = EmailQueueStatus.Pending,
                CreatedDate = DateTime.Now
            };

            _context.EmailQueue.Add(queue);

            await _context.SaveChangesAsync();
        }
    }
}