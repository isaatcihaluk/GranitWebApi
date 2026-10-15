namespace GranitWebApi.Services.Email.Models
{
    public class EmailMessageModel
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public string Subject { get; set; } = "";
        public string HtmlBody { get; set; } = "";
        public List<EmailAttachmentModel> Attachments { get; set; } = new();
    }

    public class EmailAttachmentModel
    {
        public string FileName { get; set; } = "";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}