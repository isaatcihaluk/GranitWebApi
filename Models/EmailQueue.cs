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
        public EmailQueueStatus Status { get; set; } = EmailQueueStatus.Pending;
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SentDate { get; set; }
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }
        public ICollection<EmailQueueAttachment> Attachments { get; set; } = new List<EmailQueueAttachment>();
    }
    public class EmailQueueAttachment
    {
        public int Id { get; set; }
        public int EmailQueueId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public EmailQueue EmailQueue { get; set; } = null!;
    }
}