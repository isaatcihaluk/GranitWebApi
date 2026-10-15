using GranitWebApi.Services.Email.Models;

namespace GranitWebApi.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task SendAsync(EmailMessageModel model);
    }
}