
using GranitWebApi.Data;
using GranitWebApi.Models.Helpers;
using GranitWebApi.Services.Sales;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Security.Cryptography;

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
                var order = await _context.SalesOrders.AsNoTracking()
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
                var body = SalesOrderMailRecipe.CreateBody(order.SystemOrderNumber ?? "",order.CustomerName ?? "",order.CustomerCode ?? "");
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                // 4. SİPARİŞ FORMU EXCEL
                var excelFile = await _excelService.CreateSalesOrderExcelAsync(salesOrderId);
                bodyBuilder.Attachments.Add($"SiparisFormu_{order.SystemOrderNumber}.xlsx",excelFile,
                    new ContentType("application","vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                // 5. PAKET VE KALEM BİLGİLİ SİPARİŞ GÖRSELLERİ
                await AddSalesOrderImagesAsync(bodyBuilder,salesOrderId);
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

        public async Task<SalesOrderMailResult> SendRevisionMailAsync(long salesOrderId,string revisionDescription)
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
                var order = await _context.SalesOrders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == salesOrderId);

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
                message.Subject = $"Revize - {order.SystemOrderNumber}";

                var encodedRevisionDescription = System.Net.WebUtility.HtmlEncode(revisionDescription);
                var encodedOrderNumber = System.Net.WebUtility.HtmlEncode(order.SystemOrderNumber ?? "");

                var body = $"""
                    <p>Merhaba,</p>

                    <p>
                        <strong>{encodedOrderNumber}</strong>
                        numaralı satış siparişi revize edilmiştir.
                    </p>

                    <p>
                        <strong>Revize Konusu:</strong><br/>
                        {encodedRevisionDescription}
                    </p>

                    <p>
                        Revize edilmiş sipariş formu ekte yer almaktadır.
                    </p>
                    """;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                // 4. REVİZE SİPARİŞ FORMU EXCEL
                var excelFile =await _excelService.CreateSalesOrderExcelAsync(salesOrderId,revisionDescription);
                bodyBuilder.Attachments.Add($"SiparisFormu_{order.SystemOrderNumber}.xlsx",excelFile,
                    new ContentType("application","vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                // 5. PAKET VE KALEM BİLGİLİ SİPARİŞ GÖRSELLERİ
                await AddSalesOrderImagesAsync(bodyBuilder,salesOrderId);

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
                    Aciklama = $"Revize maili gönderilirken hata oluştu: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sipariş görsellerini içeriklerine göre tekilleştirir.
        /// Ek adında paket numaralarını, kalem numaralarını ve
        /// görsel türlerini gösterir.
        ///
        /// Örnek:
        /// Paket_1_Kalem_1-2_Broşür_Ön.png
        /// Paket_1-2_Kalem_3_Koli_Üstü_Dar_1.png
        /// Kalem_4_Broşür_Ön.png
        /// </summary>
        private async Task AddSalesOrderImagesAsync(BodyBuilder bodyBuilder,long salesOrderId)
        {
            // 1. SİPARİŞ KALEMLERİ
            var lineIds = await _context.SalesOrders.AsNoTracking().Where(x => x.Id == salesOrderId)
                .SelectMany(x => x.Lines.Select(line => line.Id)).OrderBy(id => id).ToListAsync();

            var lineNumbers = lineIds .Select((lineId, index) => new {
                    LineId = lineId,
                    LineNumber = index + 1
            }).ToDictionary(x => x.LineId,x => x.LineNumber);

            if (lineNumbers.Count == 0) return;

            // 2. PAKET-KALEM İLİŞKİLERİ
            // Id, SalesOrderId, PackageNumber
            var packageLineRows = await (
                from packageLine in
                    _context.SalesOrderPackageLines.AsNoTracking()
                join package in
                    _context.SalesOrderPackages.AsNoTracking()
                    on packageLine.SalesOrderPackageId equals package.Id
                where package.SalesOrderId == salesOrderId
                select new
                {
                    packageLine.SalesOrderLineId,
                    package.PackageNumber
                }
            ).ToListAsync();

            // Her kalem hangi paketlerde kullanılıyor?
            var packagesByLine = packageLineRows.Where(x => lineNumbers.ContainsKey(x.SalesOrderLineId))
                .GroupBy(x => x.SalesOrderLineId).ToDictionary(group => group.Key,
                    group => group.Select(x => x.PackageNumber).Distinct().OrderBy(x => x).ToHashSet());

            // 3. AKTİF GÖRSELLER
            var images = await (
                from image in
                    _context.SalesOrderLineImages.AsNoTracking()
                join imageType in
                    _context.SalesOrderImageTypes.AsNoTracking()
                    on image.ImageTypeId equals imageType.Id
                where image.SalesOrderLine != null
                      && image.SalesOrderLine.SalesOrderId == salesOrderId
                      && image.IsActive
                      && imageType.IsActive
                select new
                {
                    image.Id,
                    image.SalesOrderLineId,
                    image.FileName,
                    image.FilePath,
                    ImageTypeName = imageType.Name
                }
            )
            .OrderBy(x => x.SalesOrderLineId)
            .ThenBy(x => x.Id)
            .ToListAsync();

            // Aynı dosya içeriğine sahip görseller tek grupta tutulur.
            var uniqueImages = new Dictionary<string, ImageAttachmentGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (var image in images)
            {
                if (string.IsNullOrWhiteSpace(image.FilePath) || string.IsNullOrWhiteSpace(image.FileName))
                {
                    continue;
                }
                if (!lineNumbers.TryGetValue(image.SalesOrderLineId,out var lineNumber))
                {
                    continue;
                }

                var physicalPath = Path.Combine(_fileStorageSettings.RootPath,image.FilePath);
                if (!File.Exists(physicalPath)) continue;
                var fileBytes = await File.ReadAllBytesAsync(physicalPath);
                if (fileBytes.Length == 0) continue;

                // Dosya adı veya ImageTypeId değil,
                var contentHash = Convert.ToHexString(SHA256.HashData(fileBytes));

                var imageTypeName = SanitizeAttachmentName(image.ImageTypeName);

                if (!uniqueImages.TryGetValue(contentHash,out var group))
                {
                    group = new ImageAttachmentGroup
                    {
                        FileBytes = fileBytes,
                        Extension = Path.GetExtension(image.FileName)
                    };
                    uniqueImages.Add(contentHash, group);
                }

                // Aynı görselin kullanıldığı kalemleri kaydet.
                group.LineNumbers.Add(lineNumber);

                // Aynı görsel birden fazla paket içinde kullanılıyorsa
                // paket numaralarını da birleştir.
                if (packagesByLine.TryGetValue(image.SalesOrderLineId,out var packageNumbers))
                {
                    foreach (var packageNumber in packageNumbers)
                    {
                        group.PackageNumbers.Add(packageNumber);
                    }
                }
                // Aynı içerik birden fazla görsel türünde kullanılıyorsa
                group.ImageTypeNames.Add(imageTypeName);
            }

            // 4. HER FARKLI GÖRSELİ YALNIZCA BİR KEZ EKLE
            foreach (var group in uniqueImages.Values)
            {
                var linePart = string.Join("-",group.LineNumbers.OrderBy(x => x));
                var typePart = string.Join("_",group.ImageTypeNames.OrderBy(x => x,StringComparer.OrdinalIgnoreCase));

                var packagePart = group.PackageNumbers.Count > 0
                    ? $"Paket_{string.Join("-", group.PackageNumbers.OrderBy(x => x))}_": string.Empty;

                var extension = group.Extension;
                var attachmentName = $"{packagePart}Kalem_{linePart}_{typePart}{extension}";
                bodyBuilder.Attachments.Add(attachmentName,group.FileBytes,GetContentType(attachmentName));
            }
        }

        /// <summary>
        /// İçeriği aynı olan görsellerin ilişkili paketlerini,
        /// kalemlerini ve görsel türlerini tutar.
        /// </summary>
        private sealed class ImageAttachmentGroup
        {
            public byte[] FileBytes { get; set; } = Array.Empty<byte>();
            public string Extension { get; set; } = string.Empty;
            public HashSet<int> PackageNumbers { get; } = new();
            public HashSet<int> LineNumbers { get; } = new();
            public HashSet<string> ImageTypeNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private async Task<SalesOrderMailRecipients> GetMailRecipientsAsync(string mailType)
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
                if (string.IsNullOrWhiteSpace(item.Email)) continue;
                var email = item.Email.Trim();
                switch (item.MailRole?.Trim().ToUpperInvariant())
                {
                    case "TO":
                        if (!recipients.To.Contains(email,StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.To.Add(email);
                        }
                        break;

                    case "CC":
                        if (!recipients.Cc.Contains(email,StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.Cc.Add(email);
                        }
                        break;

                    case "BCC":
                        if (!recipients.Bcc.Contains(email,StringComparer.OrdinalIgnoreCase))
                        {
                            recipients.Bcc.Add(email);
                        }
                        break;
                }
            }
            return recipients;
        }

        private static void AddRecipients(MimeMessage message,SalesOrderMailRecipients recipients)
        {
            foreach (var email in recipients.To)
                message.To.Add(MailboxAddress.Parse(email));

            foreach (var email in recipients.Cc)
                message.Cc.Add(MailboxAddress.Parse(email));

            foreach (var email in recipients.Bcc)
                message.Bcc.Add(MailboxAddress.Parse(email));
        }
        private static string SanitizeAttachmentName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Gorsel";

            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[]
                {
                    '<', '>', ':', '"', '/', '\\', '|', '?', '*'
                }).Distinct().ToHashSet();

            var result = new string(value.Trim()
                    .Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

            return string.IsNullOrWhiteSpace(result) ? "Gorsel" : result;
        }
        private static ContentType GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => new ContentType("image", "jpeg"),
                ".png" => new ContentType("image", "png"),
                ".gif" => new ContentType("image", "gif"),
                ".bmp" => new ContentType("image", "bmp"),
                ".webp" => new ContentType("image", "webp"),
                _ => new ContentType("application", "octet-stream")
            };
        }
    }
}
