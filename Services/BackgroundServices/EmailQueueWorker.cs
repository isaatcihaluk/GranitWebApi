
using GranitWebApi.Data;
using GranitWebApi.Enums;
using GranitWebApi.Services.Email;
using GranitWebApi.Services.Email.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.BackgroundServices;

public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueWorker> _logger;

    public EmailQueueWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email Queue Worker Hatası");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessQueueAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var emailService = scope.ServiceProvider
            .GetRequiredService<IEmailService>();

        var emails = await context.EmailQueue
            .Include(x => x.Attachments)
            .Where(x =>
                x.Status == EmailQueueStatus.Pending ||
                (x.Status == EmailQueueStatus.Error &&
                 x.RetryCount < 3))
            .OrderBy(x => x.CreatedDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var email in emails)
        {
            try
            {
                email.Status = EmailQueueStatus.Sending;
                await context.SaveChangesAsync(cancellationToken);

                var model = new EmailMessageModel
                {
                    To = SplitEmails(email.ToEmail),
                    Cc = SplitEmails(email.CcEmails),
                    Bcc = SplitEmails(email.BccEmails),
                    Subject = email.Subject,
                    HtmlBody = email.Body,

                    Attachments = email.Attachments
                        .Select(x => new EmailAttachmentModel
                        {
                            FileName = x.FileName,
                            ContentType = x.ContentType,
                            Content = x.Content
                        })
                        .ToList()
                };

                await emailService.SendAsync(model);

                email.Status = EmailQueueStatus.Sent;
                email.SentDate = DateTime.Now;
                email.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                email.Status = EmailQueueStatus.Error;
                email.RetryCount++;
                email.ErrorMessage = ex.Message;

                _logger.LogError(
                    ex,
                    "E-posta gönderilemedi. QueueId: {QueueId}",
                    email.Id);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<string> SplitEmails(string? emails)
    {
        if (string.IsNullOrWhiteSpace(emails))
            return new List<string>();

        return emails
            .Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
