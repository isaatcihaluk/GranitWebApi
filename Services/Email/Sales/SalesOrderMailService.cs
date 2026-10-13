using GranitWebApi.Data;
using GranitWebApi.Models.Helpers;
using GranitWebApi.Services.Sales;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GranitWebApi.Services.Email.Sales
{
    public class SalesOrderMailService : ISalesOrderMailService
    {
        private readonly AppDbContext _context;
        private readonly SalesOrderExcelService _excelService;
        private readonly EmailSettings _settings;
        private readonly FileStorageSettings _fileStorageSettings;

        private const string SalesOrderMailType = "SATIS_SIPARIS";

        public SalesOrderMailService(AppDbContext context,SalesOrderExcelService excelService,
            IOptions<EmailSettings> options,IOptions<FileStorageSettings> fileStorageOptions)
        {
            _context = context;
            _excelService = excelService;
            _settings = options.Value;
            _fileStorageSettings = fileStorageOptions.Value;
        }

        public async Task<SalesOrderMailResult> SendSalesOrderMailAsync(long salesOrderId)
        {
            try
            {
                // 1. SİPARİŞ
                var order = await _context.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == salesOrderId);

                if (order == null)
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                    };
                }

                // 2. MAIL ALICILARI
                var recipients = await GetMailRecipientsAsync(SalesOrderMailType);

                if (!recipients.To.Any() && !recipients.Cc.Any() && !recipients.Bcc.Any())
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "SATIS_SIPARIS için aktif mail alıcısı bulunamadı."
                    };
                }

                // 3. MAIL OLUŞTUR
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName,_settings.FromEmail));
                AddRecipients(message, recipients);
                message.Subject = $"Satış Siparişi - {order.SystemOrderNumber}";

                var body = SalesOrderMailRecipe.CreateBody(
                    order.SystemOrderNumber ?? "",
                    order.CustomerName ?? "",
                    order.CustomerCode ?? "");

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                // 4. SİPARİŞ FORMU EXCEL
                var excelFile = await _excelService.CreateSalesOrderExcelAsync(salesOrderId);

                bodyBuilder.Attachments.Add($"SiparisFormu_{order.SystemOrderNumber}.xlsx",
                    excelFile,new ContentType("application","vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                // 5. SİPARİŞ GÖRSELLERİ
                var images = await _context.SalesOrderLineImages.AsNoTracking()
                    .Where(x => x.SalesOrderLine != null && x.SalesOrderLine.SalesOrderId == salesOrderId &&
                        x.IsActive).ToListAsync();

                foreach (var image in images)
                {
                    if (string.IsNullOrWhiteSpace(image.FilePath))
                        continue;

                    var physicalPath = Path.Combine(_fileStorageSettings.RootPath,image.FilePath);
                    if (!File.Exists(physicalPath)) continue;

                    var fileBytes = await File.ReadAllBytesAsync(physicalPath);
                    var contentType = GetContentType(image.FileName);

                    bodyBuilder.Attachments.Add(image.FileName,fileBytes,contentType);
                }

                // 6. MAIL BODY
                message.Body = bodyBuilder.ToMessageBody();

                // 7. SMTP
                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(_settings.Host,_settings.Port,SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password);

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                // 8. BAŞARILI
                return new SalesOrderMailResult
                {
                    Basarili = true,
                    Hata = false,
                    SalesOrderId = salesOrderId,
                    Aciklama =
                        "Satış siparişi bildirim maili başarıyla gönderildi."
                };
            }
            catch (Exception ex)
            {
                return new SalesOrderMailResult
                {
                    Basarili = false,
                    Hata = true,
                    SalesOrderId = salesOrderId,
                    Aciklama =
                        $"Satış siparişi maili gönderilirken hata oluştu: {ex.Message}"
                };
            }
        }

        public async Task<SalesOrderMailResult> SendRevisionMailAsync(
            long salesOrderId,
            string revisionDescription)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(revisionDescription))
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "Revize konusu boş bırakılamaz."
                    };
                }

                // 1. SİPARİŞ
                var order = await _context.SalesOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == salesOrderId);

                if (order == null)
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama =
                            $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                    };
                }

                // 2. MAIL ALICILARI
                var recipients = await GetMailRecipientsAsync(SalesOrderMailType);

                if (!recipients.To.Any() &&
                    !recipients.Cc.Any() &&
                    !recipients.Bcc.Any())
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama =
                            "SATIS_SIPARIS için aktif mail alıcısı bulunamadı."
                    };
                }

                // 3. MAIL OLUŞTUR
                var message = new MimeMessage();

                message.From.Add(
                    new MailboxAddress(
                        _settings.FromName,
                        _settings.FromEmail));

                AddRecipients(message, recipients);

                message.Subject =
                    $"Revize - {order.SystemOrderNumber}";

                var body = $"""
                    <p>Merhaba,</p>

                    <p>
                        <strong>{order.SystemOrderNumber}</strong>
                        numaralı satış siparişi revize edilmiştir.
                    </p>

                    <p>
                        <strong>Revize Konusu:</strong><br/>
                        {System.Net.WebUtility.HtmlEncode(revisionDescription)}
                    </p>

                    <p>
                        Revize edilmiş sipariş formu ekte yer almaktadır.
                    </p>
                    """;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                // 4. EXCEL
                var excelFile =
                    await _excelService.CreateSalesOrderExcelAsync(
                        salesOrderId,
                        revisionDescription);

                bodyBuilder.Attachments.Add(
                    $"SiparisFormu_{order.SystemOrderNumber}.xlsx",
                    excelFile,
                    new ContentType(
                        "application",
                        "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                // 5. GÖRSELLER
                var images = await _context.SalesOrderLineImages
                    .AsNoTracking()
                    .Where(x =>
                        x.SalesOrderLine != null &&
                        x.SalesOrderLine.SalesOrderId == salesOrderId &&
                        x.IsActive)
                    .ToListAsync();

                foreach (var image in images)
                {
                    if (string.IsNullOrWhiteSpace(image.FilePath))
                        continue;

                    var physicalPath = Path.Combine(
                        _fileStorageSettings.RootPath,
                        image.FilePath);

                    if (!File.Exists(physicalPath))
                        continue;

                    var fileBytes =
                        await File.ReadAllBytesAsync(physicalPath);

                    var contentType =
                        GetContentType(image.FileName);

                    bodyBuilder.Attachments.Add(
                        image.FileName,
                        fileBytes,
                        contentType);
                }

                // 6. MAIL BODY
                message.Body = bodyBuilder.ToMessageBody();

                // 7. SMTP
                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password);

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                // 8. BAŞARILI
                return new SalesOrderMailResult
                {
                    Basarili = true,
                    Hata = false,
                    SalesOrderId = salesOrderId,
                    Aciklama = "Revize maili başarıyla gönderildi."
                };
            }
            catch (Exception ex)
            {
                return new SalesOrderMailResult
                {
                    Basarili = false,
                    Hata = true,
                    SalesOrderId = salesOrderId,
                    Aciklama =
                        $"Revize maili gönderilirken hata oluştu: {ex.Message}"
                };
            }
        }

        private async Task<SalesOrderMailRecipients> GetMailRecipientsAsync(
            string mailType)
        {
            var recipients = new SalesOrderMailRecipients();

            var users = await (
                from umr in _context.UserMailRoles.AsNoTracking()
                join user in _context.Users.AsNoTracking()
                    on umr.UserId equals user.Id
                where umr.MailType == mailType
                      && umr.IsActive
                select new
                {
                    user.Email,
                    umr.MailRole
                }
            ).ToListAsync();

            foreach (var item in users)
            {
                if (string.IsNullOrWhiteSpace(item.Email))
                    continue;

                var email = item.Email.Trim();

                switch (item.MailRole?.Trim().ToUpperInvariant())
                {
                    case "TO":
                        if (!recipients.To.Contains(
                                email,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.To.Add(email);
                        }
                        break;

                    case "CC":
                        if (!recipients.Cc.Contains(
                                email,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.Cc.Add(email);
                        }
                        break;

                    case "BCC":
                        if (!recipients.Bcc.Contains(
                                email,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.Bcc.Add(email);
                        }
                        break;
                }
            }

            return recipients;
        }

        private static void AddRecipients(
            MimeMessage message,
            SalesOrderMailRecipients recipients)
        {
            foreach (var email in recipients.To)
            {
                message.To.Add(MailboxAddress.Parse(email));
            }

            foreach (var email in recipients.Cc)
            {
                message.Cc.Add(MailboxAddress.Parse(email));
            }

            foreach (var email in recipients.Bcc)
            {
                message.Bcc.Add(MailboxAddress.Parse(email));
            }
        }

        private static ContentType GetContentType(string fileName)
        {
            var extension =
                Path.GetExtension(fileName)?.ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    new ContentType("image", "jpeg"),

                ".png" =>
                    new ContentType("image", "png"),

                ".gif" =>
                    new ContentType("image", "gif"),

                ".bmp" =>
                    new ContentType("image", "bmp"),

                ".webp" =>
                    new ContentType("image", "webp"),

                _ =>
                    new ContentType("application", "octet-stream")
            };
        }
    }
}