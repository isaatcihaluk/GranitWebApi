using GranitWebApi.Services.Notifications.Models;

namespace GranitWebApi.Services.Notifications
{
    public interface INotificationService
    {
        Task SendAsync(NotificationModel model);
    }
}