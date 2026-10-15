using GranitWebApi.Services.Email.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GranitWebApi.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        // Eski çağrılar geriye uyumlu kalır.
        public Task SendAsync(string to,string subject,string htmlBody)
        {
            return SendAsync(new EmailMessageModel
            {
                To = new List<string> { to },
                Subject = subject,
                HtmlBody = htmlBody
            });
        }

        // Yeni ortak mail gönderim metodu.
        public async Task SendAsync(EmailMessageModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.To == null || !model.To.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                throw new ArgumentException("En az bir alıcı belirtilmelidir.",nameof(model));
            }

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.FromName,_settings.FromEmail));

            AddRecipients(message.To, model.To);
            AddRecipients(message.Cc, model.Cc);
            AddRecipients(message.Bcc, model.Bcc);

            message.Subject = model.Subject;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = model.HtmlBody
            };

            foreach (var attachment in model.Attachments)
            {
                if (attachment.Content == null) continue;
                bodyBuilder.Attachments.Add(attachment.FileName,attachment.Content,ContentType.Parse(attachment.ContentType));
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_settings.Host,_settings.Port,SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username,_settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        private static void AddRecipients(InternetAddressList destination,IEnumerable<string>? addresses)
        {
            if (addresses == null) return;

            foreach (var address in addresses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                destination.Add(MailboxAddress.Parse(address.Trim()));
            }
        }
    }
}