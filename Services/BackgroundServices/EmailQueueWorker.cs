using GranitWebApi.Data;
using GranitWebApi.Enums;
using GranitWebApi.Services.Email;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email Queue Worker Hatası");
            }

            await Task.Delay(TimeSpan.FromSeconds(5),stoppingToken);
        }
    }
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context =scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService =scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emails = await context.EmailQueue
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
                await emailService.SendAsync(email.ToEmail,email.Subject,email.Body);

                email.Status = EmailQueueStatus.Sent;
                email.SentDate = DateTime.Now;
                email.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                email.Status = EmailQueueStatus.Error;
                email.RetryCount++;
                email.ErrorMessage = ex.Message;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}