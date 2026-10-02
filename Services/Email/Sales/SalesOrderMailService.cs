using GranitWebApi.Data;
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

        // ŞİMDİLİK TEST İÇİN SADECE KENDİ MAIL ADRESİNİ YAZ
        private const string TestRecipient = "haluk.saatci@granithome.com";

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
                        Aciklama =
                            $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                    };
                }

                // 2. TEST MAIL ALICISI
                if (string.IsNullOrWhiteSpace(TestRecipient))
                {
                    return new SalesOrderMailResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "Test mail alıcısı tanımlanmamış."
                    };
                }

                // 3. MAIL OLUŞTUR
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName,_settings.FromEmail));
                message.To.Add(MailboxAddress.Parse(TestRecipient));
                message.Subject = $"Satış Siparişi - {order.SystemOrderNumber}";
                var body = SalesOrderMailRecipe.CreateBody(
                    order.SystemOrderNumber ?? "",
                    order.CustomerName ?? "",
                    order.CustomerCode ?? "");
                var bodyBuilder = new BodyBuilder{HtmlBody = body};

                // 4. SİPARİŞ FORMU EXCEL
                var excelFile = await _excelService.CreateSalesOrderExcelAsync(salesOrderId);

                bodyBuilder.Attachments.Add($"SiparisFormu_{order.SystemOrderNumber}.xlsx",excelFile,
                    new ContentType("application","vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                // 5. SİPARİŞ GÖRSELLERİ
                var images = await _context.SalesOrderLineImages
                    .AsNoTracking()
                    .Where(x =>
                        x.SalesOrderLine != null &&
                        x.SalesOrderLine.SalesOrderId == salesOrderId &&
                        x.IsActive).ToListAsync();

                foreach (var image in images)
                {
                    if (string.IsNullOrWhiteSpace(image.FilePath)) continue;

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
                await smtp.AuthenticateAsync(_settings.Username,_settings.Password);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                // 8. BAŞARILI
                return new SalesOrderMailResult
                {
                    Basarili = true,
                    Hata = false,
                    SalesOrderId = salesOrderId,
                    Aciklama = "Satış siparişi bildirim maili başarıyla gönderildi."
                };
            }
            catch (Exception ex)
            {
                return new SalesOrderMailResult
                {
                    Basarili = false,
                    Hata = true,
                    SalesOrderId = salesOrderId,
                    Aciklama = $"Satış siparişi maili gönderilirken hata oluştu: {ex.Message}"
                };
            }
        }
        private static ContentType GetContentType(string fileName)
        {
            var extension =Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => new ContentType("image", "jpeg"),
                ".png" => new ContentType("image", "png"),
                ".gif" => new ContentType("image", "gif"),
                ".bmp" => new ContentType("image", "bmp"),
                ".webp" => new ContentType("image", "webp"),
                _ => new ContentType("application","octet-stream")
            };
        }
    }
}
