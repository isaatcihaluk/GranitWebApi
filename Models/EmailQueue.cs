using GranitWebApi.Enums;

namespace GranitWebApi.Models
{
    public class EmailQueue
    {
        public int Id { get; set; }

        public string ToEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Template { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public EmailQueueStatus Status { get; set; }
            = EmailQueueStatus.Pending;

        public int RetryCount { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;

        public DateTime? SentDate { get; set; }
    }
}