using DocumentFormat.OpenXml.ExtendedProperties;
using GranitWebApi.Data;
using GranitWebApi.Models.Helpers;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HtmlAgilityPack;

namespace GranitWebApi.Services.Proforma
{
    public interface ISalesProformaPdfService
    {
        Task<byte[]> GeneratePdfAsync(int proformaId);
        Task<SalesProformaDocumentResult> GenerateAndSaveAsync(int proformaId, int userId);
    }

    public class SalesProformaPdfService : ISalesProformaPdfService
    {
        private readonly AppDbContext _context;
        private readonly FileStorageSettings _fileStorageSettings;

        public SalesProformaPdfService(AppDbContext context, IOptions<FileStorageSettings> fileStorageOptions)
        {
            _context = context;
            _fileStorageSettings = fileStorageOptions.Value;
        }

        #region DTO
        private class ProformaPdfRow
        {
            public int ProformaId { get; set; }
            public string ProformaNumber { get; set; } = "";
            public int ProformaYear { get; set; }
            public string SystemProformaNumber { get; set; } = "";
            public string CustomerCode { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string? Tel { get; set; }
            public string? Fax { get; set; }
            public string? Email { get; set; }
            public string? DeliveryMethod { get; set; }
            public int? IncotermId { get; set; }
            public int? IncotermCode { get; set; }
            public string? IncotermDescription { get; set; }
            public DateTime? DueDate { get; set; }
            public string CurrencyCode { get; set; } = "";
            public string PaymentType { get; set; } = "";
            public decimal? AdvancePercentage { get; set; }
            public int? PaymentTermDays { get; set; }
            public string? PaymentDescription { get; set; }
            public decimal TotalAmount { get; set; }
            public string? ProformaDescription { get; set; }
            public int LineId { get; set; }
            public string? ProductCode { get; set; }
            public string ProductName { get; set; } = "";
            public string? LineDescription { get; set; }
            public decimal Quantity { get; set; }
            public string? Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountRate { get; set; }
            public string LineCurrencyCode { get; set; } = "";
            public decimal LineTotalAmount { get; set; }
            public string? BankName { get; set; }
            public string? AccountOwner { get; set; }
            public string? Branch { get; set; }
            public string? SwiftCode { get; set; }
            public string? IBAN { get; set; }
            public string? PaymentTermsText { get; set; }
            public decimal AdditionalAmount { get; set; }
            public decimal DeductionAmount { get; set; }
        }
        #endregion

        #region Generate PDF
        public async Task<byte[]> GeneratePdfAsync(int proformaId)
        {
            var rows = await GetProformaRowsAsync(proformaId);
            if (rows.Count == 0)
            {
                throw new Exception("Proforma bulunamadı veya proformaya ait aktif kalem bulunmuyor.");
            }

            var first = rows.First();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(20);
                    page.MarginBottom(25);
                    page.MarginLeft(20);
                    page.MarginRight(20);
                    page.DefaultTextStyle(TextStyle.Default.FontFamily("Arial").FontSize(8));
                    page.Header().Element(header => ComposeHeader(header, first));
                    page.Content().Element(content => ComposeContent(content, first, rows));
                    page.Footer().Element(footer => ComposeFooter(footer));
                });
            }).GeneratePdf();
        }
        #endregion

        #region Save PDF
        public async Task<SalesProformaDocumentResult> GenerateAndSaveAsync(int proformaId, int userId)
        {
            try
            {
                var proforma = await _context.SalesProformas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == proformaId);
                if (proforma == null)
                {
                    return new SalesProformaDocumentResult
                    {
                        Basarili = false,
                        Aciklama = "Proforma bulunamadı."
                    };
                }

                var pdfBytes = await GeneratePdfAsync(proformaId);
                // C:\GranitFiles
                var rootPath = _fileStorageSettings.RootPath;

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    return new SalesProformaDocumentResult
                    {
                        Basarili = false,
                        Aciklama = "FileStorage:RootPath tanımlı değil."
                    };
                }

                // C:\GranitFiles\SalesProformas\1
                var folderName = proforma.ProformaNumber ?? proformaId.ToString();
                var folderPath = Path.Combine(rootPath, "SalesProformas", folderName);
                Directory.CreateDirectory(folderPath);

                var storedFileName = $"{proforma.SystemProformaNumber}.pdf";
                var physicalPath = Path.Combine(folderPath, storedFileName);

                await File.WriteAllBytesAsync(physicalPath, pdfBytes);

                var relativePath = Path.Combine("SalesProformas", folderName, storedFileName);

                var oldDocuments = await _context.SalesProformaDocuments
                    .Where(x => x.SalesProformaId == proformaId && x.IsActive).ToListAsync();

                foreach (var oldDocument in oldDocuments)
                {
                    oldDocument.IsActive = false;
                }

                var newDocument = new SalesProformaDocument
                {
                    SalesProformaId = proformaId,
                    FileName = $"{proforma.SystemProformaNumber}.pdf",
                    StoredFileName = storedFileName,
                    FilePath = relativePath,
                    ContentType = "application/pdf",
                    FileSize = pdfBytes.LongLength,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId
                };

                _context.SalesProformaDocuments.Add(newDocument);
                await _context.SaveChangesAsync();

                return new SalesProformaDocumentResult
                {
                    Basarili = true,
                    Aciklama = "Proforma PDF başarıyla oluşturuldu.",
                    DocumentId = newDocument.Id,
                    FileName = newDocument.FileName
                };
            }
            catch (Exception ex)
            {
                return new SalesProformaDocumentResult
                {
                    Basarili = false,
                    Aciklama = $"Proforma PDF oluşturulurken hata oluştu: {ex.Message}"
                };
            }
        }
        #endregion

        #region SQL
        private async Task<List<ProformaPdfRow>> GetProformaRowsAsync(int proformaId)
        {
            var sql = @"
                SELECT
                    P.Id AS ProformaId,
                    P.ProformaNumber,
                    P.ProformaYear,
                    P.SystemProformaNumber,
                    P.CustomerCode,
                    P.CustomerName,
                    CA.CARI_TEL AS Tel,
                    CA.FAX,
                    CA.EMAIL,
                    P.DeliveryMethod,
                    P.IncotermId,
                    I.ID AS IncotermCode,
                    I.Name AS IncotermDescription,
                    P.DueDate,
                    P.CurrencyCode,
                    P.PaymentType,
                    P.AdvancePercentage,
                    P.PaymentTermDays,
                    P.PaymentDescription,
                    P.PaymentTermsText,
                    P.TotalAmount,
                    P.AdditionalAmount,
                    P.DeductionAmount,
                    P.Description AS ProformaDescription,
                    L.Id AS LineId,
                    L.ProductCode,
                    L.ProductName,
                    L.Description AS LineDescription,
                    L.Quantity,
                    L.Unit,
                    L.UnitPrice,
                    L.CurrencyCode AS LineCurrencyCode,
                    L.TotalAmount AS LineTotalAmount,
                    L.DiscountRate AS DiscountRate,
                    B.BankName,
                    B.AccountOwner,
                    B.Branch,
                    B.SwiftCode,
                    B.IBAN
                FROM SalesProformas P
                INNER JOIN SalesProformaLines L ON L.ProformaId = P.Id AND L.DeletedFlag = 0
                LEFT JOIN EGEKIRAN2026..TBLCASABIT CA ON CA.CARI_KOD = P.CustomerCode
                LEFT JOIN Incoterms I ON I.Id = P.IncotermId
                LEFT JOIN BankAccounts B ON B.Id = P.BankAccountId
                WHERE P.Id = {0}
                ORDER BY L.Id;
                ";
            return await _context.Database.SqlQueryRaw<ProformaPdfRow>(sql, proformaId).ToListAsync();
        }
        #endregion

        #region Header
        private void ComposeHeader(IContainer container, ProformaPdfRow model)
        {
            container.Border(1).BorderColor(Colors.Black).Column(column =>
            {
                column.Item().Padding(7).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("EGE KIRAN").Bold().FontSize(18);
                        left.Item().PaddingTop(2).Text("EGE KIRAN İTHALAT İHRACAT PAZARLAMA LTD ŞTİ").Bold().FontSize(7);
                        left.Item().PaddingTop(2).Text("ORGANİZE SANAYİ BÖLGESİ MAH. 2. CAD. NO: 4 TORBALI / İZMİR").FontSize(7);
                        left.Item().Text("TEL: (0232) 853 91 00 (PBX)   FAX: (0232) 853 85 86").FontSize(7);
                        left.Item().Text("e-mail : info@granithome.com").FontSize(7);
                    });
                });

                column.Item().BorderTop(1).BorderBottom(1).Background(Colors.Grey.Lighten3).PaddingVertical(5)
                .AlignCenter().Text("PROFORMA INVOICE").Bold().FontSize(12);
            });
        }
        #endregion

        #region Stamp
        private void ComposeStamp(IContainer container)
        {
            var stampPath = Path.Combine(_fileStorageSettings.RootPath, _fileStorageSettings.ProformaStampPath);
            if (!File.Exists(stampPath))
            {
                container.AlignCenter().AlignMiddle().Text("EGE KIRAN").Bold().FontSize(8);
                return;
            }
            container.AlignCenter().AlignMiddle().MaxHeight(43).MaxWidth(150).Image(File.ReadAllBytes(stampPath)).FitArea();
        }
        #endregion

        #region Content
        private void ComposeContent(IContainer container, ProformaPdfRow first, List<ProformaPdfRow> rows)
        {
            container.BorderLeft(1).BorderRight(1).BorderBottom(1).Column(column =>
            {
                ComposeCustomerSection(column, first);
                ComposeLinesTable(column, rows);
                ComposeNotes(column, first);
                ComposePaymentSection(column, first);
                ComposeTerms(column, first);
            });
        }
        #endregion

        #region Customer
        private void ComposeCustomerSection(ColumnDescriptor column, ProformaPdfRow model)
        {
            column.Item().BorderBottom(1).Padding(6).Row(row =>
            {
                row.RelativeItem(3).Column(customer =>
                {
                    customer.Item().Text(text =>
                    {
                        text.Span("CUSTOMER : ").Bold();
                        text.Span(model.CustomerName);
                    });

                    customer.Item().Text(text =>
                    {
                        text.Span("CODE : ").Bold();
                        text.Span(model.CustomerCode);
                    });

                    customer.Item().Text(text =>
                    {
                        text.Span("TEL : ").Bold();
                        text.Span(model.Tel ?? "-");
                    });

                    customer.Item().Text(text =>
                    {
                        text.Span("FAX : ").Bold();
                        text.Span(model.Fax ?? "-");
                    });

                    customer.Item().Text(text =>
                    {
                        text.Span("E-MAIL : ").Bold();
                        text.Span(model.Email ?? "-");
                    });
                });

                row.RelativeItem(1.4f).BorderLeft(1).PaddingLeft(7).Column(info =>
                {
                    info.Item().Text(text =>
                    {
                        text.Span("NUMBER : ").Bold();
                        text.Span(model.SystemProformaNumber);
                    });

                    info.Item().Text(text =>
                    {
                        text.Span("DATE : ").Bold();
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });
                });
            });
        }
        #endregion

        #region Lines
        private void ComposeLinesTable(ColumnDescriptor column, List<ProformaPdfRow> rows)
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(55);
                    columns.RelativeColumn(3.5f);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(75);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "NO");
                    HeaderCell(header.Cell(), "CODE");
                    HeaderCell(header.Cell(), "DESCRIPTION OF GOODS");
                    HeaderCell(header.Cell(), "QUANTITY");
                    HeaderCell(header.Cell(), "UNIT");
                    HeaderCell(header.Cell(), "UNIT PRICE");
                    HeaderCell(header.Cell(), "TOTAL VALUE");
                });

                var index = 1;
                decimal subtotal = 0m;
                decimal totalDiscount = 0m;

                foreach (var line in rows)
                {
                    // İndirimsiz satır toplamı
                    var grossTotal = line.LineTotalAmount;

                    // Satır indirimi
                    var discountAmount =
                        grossTotal * line.DiscountRate / 100m;

                    subtotal += grossTotal;
                    totalDiscount += discountAmount;

                    table.Cell()
                        .Border(1).Padding(4)
                        .AlignCenter()
                        .Text(index.ToString())
                        .Bold();

                    table.Cell()
                        .Border(1).Padding(4)
                        .Text(line.ProductCode ?? "");

                    table.Cell()
                        .Border(1).Padding(4)
                        .Column(description =>
                        {
                            description.Item()
                                .Text(line.ProductName ?? "")
                                .Bold();

                            if (!string.IsNullOrWhiteSpace(line.LineDescription))
                            {
                                foreach (var text in SplitLines(line.LineDescription))
                                {
                                    description.Item()
                                        .PaddingTop(2)
                                        .Text("• " + text)
                                        .FontSize(7);
                                }
                            }
                        });

                    table.Cell()
                        .Border(1).Padding(4)
                        .AlignCenter()
                        .Text(FormatDecimal(line.Quantity));

                    table.Cell()
                        .Border(1).Padding(4)
                        .AlignCenter()
                        .Text("PCS");

                    table.Cell()
                        .Border(1).Padding(4)
                        .AlignRight()
                        .Text(FormatMoney(line.UnitPrice, line.LineCurrencyCode));

                    // Ürün satırında indirimsiz toplam gösterilir.
                    table.Cell()
                        .Border(1).Padding(4)
                        .AlignRight()
                        .Text(FormatMoney(grossTotal, line.LineCurrencyCode));

                    index++;
                }

                var first = rows.First();
                var currencyCode = first.CurrencyCode ?? "USD";

                // İndirimsiz kalem toplamı
                AddTotalRow(table, "SUBTOTAL", subtotal, currencyCode);

                // İndirim varsa göster
                if (totalDiscount != 0)
                {
                    AddTotalRow(
                        table,
                        "DISCOUNT",
                        -totalDiscount,
                        currencyCode);
                }

                // İndirim sonrası toplam
                var totalAfterDiscount = subtotal - totalDiscount;

                // Ek tutar sıfır değilse göster
                if (first.AdditionalAmount != 0)
                {
                    AddTotalRow(
                        table,
                        "ADDITIONAL AMOUNT",
                        first.AdditionalAmount,
                        currencyCode);
                }

                // Düşülecek tutar sıfır değilse göster
                if (first.DeductionAmount != 0)
                {
                    AddTotalRow(
                        table,
                        "DEDUCTION AMOUNT",
                        -first.DeductionAmount,
                        currencyCode);
                }

                // PDF'de gösterilecek nihai tutar
                var grandTotal =
                    totalAfterDiscount
                    + first.AdditionalAmount
                    - first.DeductionAmount;

                AddTotalRow(
                    table,
                    "GRAND TOTAL",
                    grandTotal,
                    currencyCode,
                    true);
            });
        }

        private void AddTotalRow(TableDescriptor table, string label, decimal amount, string currencyCode, bool emphasize = false)
        {
            table.Cell().ColumnSpan(5).Border(1).Padding(5).AlignRight().Text(label).Bold();
            var value = table.Cell().ColumnSpan(2).Border(1).Padding(5).AlignRight().Text(FormatMoney(amount, currencyCode));
            value.Bold();
            if (emphasize) value.FontSize(10);
        }

        private void HeaderCell(IContainer container, string text)
        {
            container.Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter()
                .AlignMiddle().Text(text).Bold().FontSize(7);
        }
        #endregion



        #region Notes
        private void ComposeNotes(ColumnDescriptor column, ProformaPdfRow model)
        {
            column.Item().AlignLeft().Padding(6).Column(notes => {
                // Incoterm bilgisi
                notes.Item()
                    .PaddingTop(3)
                    .Text(text =>
                    {
                        text.Span("INCOTERM : ").Bold();
                        text.Span(model.IncotermDescription ?? "-");
                    });

                // DevExpress HTML Editor'dan gelen açıklama
                if (!string.IsNullOrWhiteSpace(model.ProformaDescription))
                {
                    notes.Item()
                        .Text("NOTES")
                        .Bold()
                        .FontSize(9);

                    notes.Item().PaddingTop(3).Element(container =>
                    {
                        ComposeHtmlDescription(container, model.ProformaDescription);
                    });
                }
                notes.Item()
            .PaddingTop(6)
            .Border(1)
            .Padding(5)
            .AlignCenter()
            .Text("PRICES IN THIS PROFORMA DO NOT COVER ANY INSPECTION FEE & ANY ARTWORK COST WHICH MAY BE REQUESTED BY THE BUYER LATER ON")
            .Bold()
            .FontSize(7);

            });
        }
        #endregion



        #region Payment
        private void ComposePaymentSection(ColumnDescriptor column, ProformaPdfRow model)
        {
            column.Item().BorderTop(1).Padding(6).Column(payment =>
            {
                payment.Item().Text(text =>
                {
                    text.Span("PAYMENT : ").Bold();
                    text.Span(GetPaymentText(model));
                });

                if (!string.IsNullOrWhiteSpace(model.PaymentDescription))
                {
                    payment.Item().PaddingTop(3).Text(text =>
                    {
                        text.Span("PAYMENT DESCRIPTION : ").Bold();
                        text.Span(model.PaymentDescription);
                    });
                }
                if (model.DueDate.HasValue)
                {
                    payment.Item().Text(text =>
                    {
                        text.Span("DELIVERY DATE : ").Bold();
                        text.Span(model.DueDate.Value.ToString("dd.MM.yyyy"));
                    });
                }

                payment.Item().Row(row =>
                {
                    row.RelativeItem().Column(bank =>
                    {
                        bank.Item().Text(text =>
                        {
                            text.Span("OUR BANK : ").Bold();
                            text.Span(model.BankName ?? "-");
                        });

                        bank.Item().Text(text =>
                        {
                            text.Span("ACCOUNT OWNER : ").Bold();
                            text.Span(model.AccountOwner ?? "-");
                        });

                        bank.Item().Text(text =>
                        {
                            text.Span("BRANCH : ").Bold();
                            text.Span(model.Branch ?? "-");
                        });

                        bank.Item().Text(text =>
                        {
                            text.Span("SWIFT : ").Bold();
                            text.Span(model.SwiftCode ?? "-");
                        });

                        bank.Item().Text(text =>
                        {
                            text.Span("IBAN : ").Bold();
                            text.Span(model.IBAN ?? "-");
                        });
                    });
                });
            });
        }

        private string GetPaymentText(ProformaPdfRow model)
        {
            if (!string.IsNullOrWhiteSpace(model.PaymentTermsText)) return model.PaymentTermsText.Trim();
            return model.PaymentType ?? string.Empty;
        }
        #endregion

        #region Terms
        private void ComposeTerms(ColumnDescriptor column, ProformaPdfRow model)
        {
            column.Item().BorderTop(1).Padding(5).Column(terms =>
            {
                terms.Item().AlignCenter()
                    .Text("The undersigned seller and buyer have agreed to close the following " +
                    "transaction according to the terms and condition stipulated as below:")
                    .Bold().FontSize(7);

                terms.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // Başlıklar
                    table.Cell().Border(1).Padding(4).AlignCenter().Text("SELLER COMPANY NAME").Bold().FontSize(7);
                    table.Cell().Border(1).Padding(4).AlignCenter().Text("BUYER COMPANY NAME").Bold().FontSize(7);

                    // Satıcı kutusu: kaşe yalnızca burada
                    table.Cell().Border(1).Height(78).Padding(4).Column(seller =>
                    {
                        seller.Item().AlignCenter().Text("EGE KIRAN İTHALAT İHRACAT " + "PAZARLAMA LTD ŞTİ").FontSize(7);
                        seller.Item().AlignCenter().AlignMiddle().Height(43).Element(ComposeStamp);
                        seller.Item().AlignCenter().Text("AUTHORIZED SIGNATURE").FontSize(6);
                    });

                    // Alıcı kutusu
                    table.Cell().Border(1).Height(78).Padding(4).AlignCenter().AlignMiddle().Text(model.CustomerName).FontSize(8);
                });
            });
        }
        #endregion

        #region Footer
        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(5).AlignCenter().Text(text =>
            {
                text.Span("PROFORMA INVOICE").Bold();
                text.Span("   |   ");
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        }
        #endregion

        #region Helpers
        private static IEnumerable<string> SplitLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var value = line.Trim();
                if (!string.IsNullOrWhiteSpace(value)) yield return value;
            }
        }

        private static string FormatDecimal(decimal value)
        {
            return value.ToString("#,##0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string FormatMoney(decimal value, string? currency)
        {
            var symbol = currency?.ToUpperInvariant() switch
            {
                "EUR" => "€",
                "USD" => "$",
                "GBP" => "£",
                "TRY" => "₺",
                _ => currency ?? ""
            };

            return $"{value:N2} {symbol}";
        }


        private static void ComposeHtmlDescription(
            IContainer container,
            string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            container.Column(column =>
            {
                foreach (var node in document.DocumentNode.ChildNodes)
                {
                    RenderHtmlNode(column, node);
                }
            });
        }

        private static void RenderHtmlNode(
            ColumnDescriptor column,
            HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(node.InnerText).Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    column.Item()
                        .Text(text)
                        .FontSize(7);
                }

                return;
            }

            if (node.NodeType != HtmlNodeType.Element)
                return;

            switch (node.Name.ToLowerInvariant())
            {
                case "p":
                case "div":
                    column.Item().PaddingBottom(3).Element(container =>
                    {
                        ComposeHtmlInlineContent(container, node);
                    });
                    break;

                case "ul":
                case "ol":
                    var index = 1;

                    foreach (var child in node.ChildNodes.Where(
                        x => x.Name.Equals("li", StringComparison.OrdinalIgnoreCase)))
                    {
                        var currentIndex = index++;

                        column.Item()
                            .PaddingLeft(8)
                            .PaddingBottom(2)
                            .Row(row =>
                            {
                                row.ConstantItem(12)
                                    .Text(node.Name.Equals(
                                        "ol",
                                        StringComparison.OrdinalIgnoreCase)
                                        ? $"{currentIndex}."
                                        : "•")
                                    .FontSize(7);

                                row.RelativeItem().Element(container =>
                                {
                                    ComposeHtmlInlineContent(container, child);
                                });
                            });
                    }
                    break;

                case "br":
                    column.Item().Height(3);
                    break;

                default:
                    ComposeHtmlInlineContent(
                        column.Item(),
                        node);
                    break;
            }
        }

        private static void ComposeHtmlInlineContent(
            IContainer container,
            HtmlNode node)
        {
            container.Text(text =>
            {
                AddHtmlSpans(text, node, false, false, false);
            });
        }

        private static void AddHtmlSpans(
            TextDescriptor text,
            HtmlNode node,
            bool bold,
            bool italic,
            bool underline)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Text)
                {
                    var value = HtmlEntity.DeEntitize(child.InnerText);

                    if (string.IsNullOrEmpty(value))
                        continue;

                    var span = text.Span(value).FontSize(7);

                    if (bold)
                        span.Bold();

                    if (italic)
                        span.Italic();

                    if (underline)
                        span.Underline();

                    continue;
                }

                if (child.NodeType != HtmlNodeType.Element)
                    continue;

                var tag = child.Name.ToLowerInvariant();

                AddHtmlSpans(
                    text,
                    child,
                    bold || tag == "b" || tag == "strong",
                    italic || tag == "i" || tag == "em",
                    underline || tag == "u");
            }
        }

        #endregion
    }
}