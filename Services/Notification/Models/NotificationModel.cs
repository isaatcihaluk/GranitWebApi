
using GranitWebApi.Services.Email.Models;

namespace GranitWebApi.Services.Notifications.Models
{
    public class NotificationModel
    {
        // Mevcut kullanımlarla geriye uyumluluk
        public string To { get; set; } = "";
        public List<string> ToEmails { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public string Subject { get; set; } = "";
        public string Template { get; set; } = "";
        public Dictionary<string, string> Values { get; set; } = new();
        public List<EmailAttachmentModel> Attachments { get; set; } = new();
    }
}
