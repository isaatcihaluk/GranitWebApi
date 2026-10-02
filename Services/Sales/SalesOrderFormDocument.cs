using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderFormDocument : IDocument
    {
        private readonly SalesOrderForm _order;
        private readonly string _rootPath;

        public SalesOrderFormDocument(
            SalesOrderForm order,
            string rootPath)
        {
            _order = order;
            _rootPath = rootPath;
        }

        public DocumentMetadata GetMetadata()
        {
            return new DocumentMetadata
            {
                Title = $"Sipariş Formu - {_order.SystemOrderNumber}",
                Author = "Granit ERP",
                Subject = "Satış Sipariş Formu"
            };
        }

        // =========================================================
        // DOCUMENT
        // =========================================================

        public void Compose(IDocumentContainer container)
        {
            // =====================================================
            // 1. SAYFA - SİPARİŞ ÖZETİ
            // =====================================================

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);

                page.DefaultTextStyle(x =>
                    x.FontSize(8));

                page.Header()
                    .Element(ComposeHeader);

                page.Content()
                    .Element(ComposeSummaryPage);

                page.Footer()
                    .Element(ComposeFooter);
            });

            // =====================================================
            // 2+ SAYFALAR - HER PAKET AYRI SAYFA
            // =====================================================

            foreach (var package in _order.Packages)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);

                    page.DefaultTextStyle(x =>
                        x.FontSize(8));

                    page.Header()
                        .Element(ComposeHeader);

                    page.Content()
                        .Element(x =>
                            ComposePackagePage(
                                x,
                                package));

                    page.Footer()
                        .Element(ComposeFooter);
                });
            }
        }

        // =========================================================
        // HEADER
        // =========================================================

        private void ComposeHeader(IContainer container)
        {
            container
                .Column(column =>
                {
                    column.Item()
                        .PaddingBottom(8)
                        .Row(row =>
                        {
                            // -------------------------------------------------
                            // SOL - GRANİT
                            // -------------------------------------------------

                            row.RelativeItem()
                                .AlignLeft()
                                .Column(headerColumn =>
                                {
                                    headerColumn.Item()
                                        .Text("GRANİT")
                                        .FontSize(20)
                                        .Bold();

                                    headerColumn.Item()
                                        .PaddingTop(2)
                                        .Text("SATIŞ SİPARİŞ FORMU")
                                        .FontSize(10)
                                        .Bold();
                                });

                            // -------------------------------------------------
                            // SAĞ - SİPARİŞ NO
                            // -------------------------------------------------

                            row.ConstantItem(220)
                                .AlignRight()
                                .Column(headerColumn =>
                                {
                                    headerColumn.Item()
                                        .AlignRight()
                                        .Text("SİPARİŞ NO")
                                        .FontSize(7)
                                        .Bold();

                                    headerColumn.Item()
                                        .PaddingTop(2)
                                        .AlignRight()
                                        .Text(_order.SystemOrderNumber ?? "-")
                                        .FontSize(12)
                                        .Bold();

                                    headerColumn.Item()
                                        .PaddingTop(2)
                                        .AlignRight()
                                        .Text(
                                            _order.DueDate.HasValue
                                                ? $"Termin: {_order.DueDate.Value:dd.MM.yyyy}"
                                                : "")
                                        .FontSize(8);
                                });
                        });

                    column.Item()
                        .LineHorizontal(1);
                });
        }

        // =========================================================
        // FOOTER
        // =========================================================

        private void ComposeFooter(IContainer container)
        {
            container
                .Column(column =>
                {
                    column.Item()
                        .PaddingTop(6)
                        .LineHorizontal(0.5f);

                    column.Item()
                        .PaddingTop(4)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Granit ERP - Satış Sipariş Formu")
                                .FontSize(7);

                            row.RelativeItem()
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.Span("Sayfa ")
                                        .FontSize(7);

                                    text.CurrentPageNumber()
                                        .FontSize(7);

                                    text.Span(" / ")
                                        .FontSize(7);

                                    text.TotalPages()
                                        .FontSize(7);
                                });
                        });
                });
        }

        // =========================================================
        // 1. SAYFA
        // =========================================================

        private void ComposeSummaryPage(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(12);

                // -------------------------------------------------
                // SİPARİŞ BİLGİLERİ
                // -------------------------------------------------

                column.Item()
                    .Element(ComposeOrderInformation);

                // -------------------------------------------------
                // PAKET ÖZETİ
                // -------------------------------------------------

                column.Item()
                    .Element(ComposePackageSummary);
            });
        }

        // =========================================================
        // SİPARİŞ BİLGİLERİ
        // =========================================================

        private void ComposeOrderInformation(IContainer container)
        {
            container
                .Border(0.8f)
                .Column(column =>
                {
                    // -------------------------------------------------
                    // BAŞLIK
                    // -------------------------------------------------

                    column.Item()
                        .Background(Colors.Grey.Lighten2)
                        .Padding(7)
                        .Text("SİPARİŞ BİLGİLERİ")
                        .Bold()
                        .FontSize(10);

                    // -------------------------------------------------
                    // BİLGİLER
                    // -------------------------------------------------

                    column.Item()
                        .Padding(8)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(85);
                                columns.RelativeColumn(1.3f);

                                columns.ConstantColumn(85);
                                columns.RelativeColumn(1.3f);

                                columns.ConstantColumn(85);
                                columns.RelativeColumn(1.3f);
                            });

                            InfoCell(
                                table,
                                "Müşteri Kodu",
                                _order.CustomerCode);

                            InfoCell(
                                table,
                                "Müşteri",
                                _order.CustomerName);

                            InfoCell(
                                table,
                                "Satış Temsilcisi",
                                _order.SalesRepresentativeName);

                            InfoCell(
                                table,
                                "Satış Tipi",
                                _order.SalesType);

                            InfoCell(
                                table,
                                "Sipariş Tipi",
                                _order.OrderType);

                            InfoCell(
                                table,
                                "Termin",
                                _order.DueDate?
                                    .ToString("dd.MM.yyyy"));

                            InfoCell(
                                table,
                                "Teslimat",
                                _order.DeliveryMethod);

                            InfoCell(
                                table,
                                "Incoterm",
                                _order.Incoterm);

                            InfoCell(
                                table,
                                "Sipariş No",
                                _order.SystemOrderNumber);
                        });
                });
        }

        private static void InfoCell(
            TableDescriptor table,
            string label,
            string? value)
        {
            table.Cell()
                .Border(0.5f)
                .Background(Colors.Grey.Lighten4)
                .Padding(5)
                .AlignMiddle()
                .Text(label)
                .Bold()
                .FontSize(7.5f);

            table.Cell()
                .Border(0.5f)
                .Padding(5)
                .AlignMiddle()
                .Text(value ?? "-")
                .FontSize(8);
        }

        // =========================================================
        // PAKET ÖZETİ
        // =========================================================

        private void ComposePackageSummary(IContainer container)
        {
            container
                .Column(column =>
                {
                    // -------------------------------------------------
                    // BAŞLIK
                    // -------------------------------------------------

                    column.Item()
                        .Background(Colors.Grey.Lighten2)
                        .Padding(7)
                        .Text("PAKET ÖZETİ")
                        .Bold()
                        .FontSize(10);

                    // -------------------------------------------------
                    // TABLO
                    // -------------------------------------------------

                    column.Item()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);       // No
                                columns.RelativeColumn(1.7f);     // Paket Kod
                                columns.RelativeColumn(2.4f);     // Paket Ad
                                columns.ConstantColumn(55);       // Miktar
                                columns.RelativeColumn(1.4f);     // Gövde
                                columns.RelativeColumn(1.4f);     // Kanat
                                columns.RelativeColumn(2.4f);     // Renk
                                columns.RelativeColumn(2.4f);     // Plastik
                                columns.RelativeColumn(1.5f);     // Shrink
                                columns.RelativeColumn(1.7f);     // Koli
                            });

                            HeaderCell(table, "No");
                            HeaderCell(table, "Paket Kod");
                            HeaderCell(table, "Paket Ad");
                            HeaderCell(table, "Miktar");
                            HeaderCell(table, "Gövde");
                            HeaderCell(table, "Kanat");
                            HeaderCell(table, "Renk");
                            HeaderCell(table, "Plastik");
                            HeaderCell(table, "Shrink");
                            HeaderCell(table, "Koli");

                            foreach (var package in _order.Packages)
                            {
                                var ck = package.CKDetails;

                                BodyCell(
                                    table,
                                    package.PackageNumber.ToString());

                                BodyCell(
                                    table,
                                    package.NetsisPaketKodu);

                                BodyCell(
                                    table,
                                    package.NetsisPaketAdi);

                                BodyCell(
                                    table,
                                    package.Miktar.ToString("0.###"));

                                BodyCell(
                                    table,
                                    ck?.GovdeDetay);

                                BodyCell(
                                    table,
                                    ck?.KanatDetay);

                                BodyCell(
                                    table,
                                    FormatColors(ck));

                                BodyCell(
                                    table,
                                    FormatPlastics(ck));

                                BodyCell(
                                    table,
                                    ck?.ShrinkDurum);

                                BodyCell(
                                    table,
                                    FormatKoli(package));
                            }
                        });
                });
        }

        // =========================================================
        // PAKET SAYFASI
        // =========================================================

        private void ComposePackagePage(
            IContainer container,
            SalesOrderFormPackage package)
        {
            var ck = package.CKDetails;

            container.Column(column =>
            {
                column.Spacing(10);

                // -------------------------------------------------
                // PAKET BAŞLIĞI
                // -------------------------------------------------

                column.Item()
                    .Border(0.8f)
                    .Background(Colors.Grey.Lighten2)
                    .Padding(8)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text(
                                $"PAKET {package.PackageNumber}")
                            .Bold()
                            .FontSize(13);

                        row.ConstantItem(260)
                            .AlignRight()
                            .Text(
                                $"KOLİ KODU: {package.KoliKod ?? "-"}")
                            .FontSize(8)
                            .Bold();
                    });

                // -------------------------------------------------
                // PAKET GENEL BİLGİLERİ
                // -------------------------------------------------

                column.Item()
                    .Element(x =>
                        ComposePackageInformation(
                            x,
                            package,
                            ck));

                // -------------------------------------------------
                // ÜRÜN BİLGİLERİ
                // -------------------------------------------------

                column.Item()
                    .Element(x =>
                        ComposePackageProducts(
                            x,
                            package));

                // -------------------------------------------------
                // TEKNİK BİLGİLER
                // -------------------------------------------------

                if (ck != null)
                {
                    column.Item()
                        .Element(x =>
                            ComposeTechnicalInformation(
                                x,
                                ck));
                }

                // -------------------------------------------------
                // GÖRSELLER
                // -------------------------------------------------

                column.Item()
                    .Element(x =>
                        ComposePackageImages(
                            x,
                            package));
            });
        }

        // =========================================================
        // PAKET GENEL BİLGİLERİ
        // =========================================================

        private void ComposePackageInformation(
            IContainer container,
            SalesOrderFormPackage package,
            SalesOrderFormCKDetails? ck)
        {
            container
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn(2f);

                        columns.ConstantColumn(80);
                        columns.RelativeColumn(2f);

                        columns.ConstantColumn(80);
                        columns.RelativeColumn(1f);
                    });

                    InfoHeaderCell(table, "Paket Kod");
                    InfoValueCell(table, package.NetsisPaketKodu);

                    InfoHeaderCell(table, "Paket Ad");
                    InfoValueCell(table, package.NetsisPaketAdi);

                    InfoHeaderCell(table, "Miktar");
                    InfoValueCell(
                        table,
                        package.Miktar.ToString("0.###"));

                    InfoHeaderCell(table, "Koli Kod");
                    InfoValueCell(table, package.KoliKod);

                    InfoHeaderCell(table, "Koli İçeriği");
                    InfoValueCell(
                        table,
                        package.KoliIciMiktar > 0
                            ? $"{package.KoliIciMiktar}'li"
                            : "-");

                    InfoHeaderCell(table, "Koli Durumu");
                    InfoValueCell(table, package.KoliDurum);
                });
        }

        // =========================================================
        // PAKET ÜRÜNLERİ
        // =========================================================

        private void ComposePackageProducts(
            IContainer container,
            SalesOrderFormPackage package)
        {
            container.Column(column =>
            {
                column.Item()
                    .PaddingBottom(5)
                    .Text("ÜRÜN BİLGİLERİ")
                    .Bold()
                    .FontSize(10);

                column.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(2.8f);
                            columns.RelativeColumn(3f);
                        });

                        HeaderCell(table, "No");
                        HeaderCell(table, "Ürün");
                        HeaderCell(table, "Katalog");
                        HeaderCell(table, "Shrinkli Kod");
                        HeaderCell(table, "Shrinkli Ad");

                        foreach (var line in package.Lines)
                        {
                            BodyCell(
                                table,
                                line.SalesOrderLineId.ToString());

                            BodyCell(
                                table,
                                line.ProductName);

                            BodyCell(
                                table,
                                line.CatalogCode);

                            BodyCell(
                                table,
                                line.ShrinkliKod);

                            BodyCell(
                                table,
                                line.ShrinkliAd);
                        }
                    });
            });
        }

        // =========================================================
        // TEKNİK BİLGİLER
        // =========================================================

        private void ComposeTechnicalInformation(
            IContainer container,
            SalesOrderFormCKDetails ck)
        {
            container
                .Column(column =>
                {
                    column.Item()
                        .PaddingBottom(5)
                        .Text("TEKNİK BİLGİLER")
                        .Bold()
                        .FontSize(10);

                    column.Item()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn(1.5f);

                                columns.ConstantColumn(90);
                                columns.RelativeColumn(1.5f);

                                columns.ConstantColumn(90);
                                columns.RelativeColumn(1.5f);
                            });

                            InfoHeaderCell(
                                table,
                                "Gövde Detayı");

                            InfoValueCell(
                                table,
                                ck.GovdeDetay);

                            InfoHeaderCell(
                                table,
                                "Kanat Detayı");

                            InfoValueCell(
                                table,
                                ck.KanatDetay);

                            InfoHeaderCell(
                                table,
                                "Ayak RAL");

                            InfoValueCell(
                                table,
                                FormatRal(
                                    ck.AyakRal,
                                    ck.AyakRalDetay));

                            InfoHeaderCell(
                                table,
                                "Gövde / Kanat RAL");

                            InfoValueCell(
                                table,
                                FormatRal(
                                    ck.GovdeKanatRal,
                                    ck.GovdeKanatRalDetay));

                            InfoHeaderCell(
                                table,
                                "Plastik 1");

                            InfoValueCell(
                                table,
                                FormatPlastic(
                                    ck.PlastikRenk1No,
                                    ck.PlastikRenk1));

                            InfoHeaderCell(
                                table,
                                "Plastik 2");

                            InfoValueCell(
                                table,
                                FormatPlastic(
                                    ck.PlastikRenk2No,
                                    ck.PlastikRenk2));

                            InfoHeaderCell(
                                table,
                                "Shrink");

                            InfoValueCell(
                                table,
                                ck.ShrinkKod);

                            InfoHeaderCell(
                                table,
                                "Shrink Ürün");

                            InfoValueCell(
                                table,
                                ck.ShrinkUrunAdi);

                            InfoHeaderCell(
                                table,
                                "Shrink Durum");

                            InfoValueCell(
                                table,
                                ck.ShrinkDurum);
                        });
                });
        }

        // =========================================================
        // GÖRSELLER
        // =========================================================

        private void ComposePackageImages(
            IContainer container,
            SalesOrderFormPackage package)
        {
            var images = package.Lines
                .SelectMany(x => x.Images)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.FilePath))
                .Where(IsImageFile)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();

            if (!images.Any())
                return;

            var brochureImages = images
                .Where(IsBrochureImage)
                .ToList();

            var otherImages = images
                .Where(x => !IsBrochureImage(x))
                .ToList();

            container.Column(column =>
            {
                // =================================================
                // BROŞÜR
                // =================================================

                if (brochureImages.Any())
                {
                    column.Item()
                        .PaddingTop(5)
                        .Text("BROŞÜR")
                        .Bold()
                        .FontSize(10);

                    foreach (var image in brochureImages)
                    {
                        var physicalPath =
                            GetPhysicalFilePath(
                                image.FilePath!);

                        if (!File.Exists(physicalPath))
                            continue;

                        column.Item()
                            .PaddingTop(5)
                            .AlignCenter()
                            .MaxHeight(330)
                            .Image(physicalPath)
                            .FitArea();
                    }
                }

                // =================================================
                // DİĞER GÖRSELLER
                // =================================================

                if (otherImages.Any())
                {
                    column.Item()
                        .PaddingTop(10)
                        .Text("ÜRÜN GÖRSELLERİ")
                        .Bold()
                        .FontSize(10);

                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            foreach (var image in otherImages)
                            {
                                var physicalPath =
                                    GetPhysicalFilePath(
                                        image.FilePath!);

                                if (!File.Exists(physicalPath))
                                    continue;

                                row.RelativeItem()
                                    .PaddingRight(8)
                                    .Column(imageColumn =>
                                    {
                                        imageColumn.Item()
                                            .Text(
                                                image.ImageTypeName ??
                                                image.FileName)
                                            .FontSize(7.5f)
                                            .Bold();

                                        imageColumn.Item()
                                            .PaddingTop(3)
                                            .Height(100)
                                            .Image(physicalPath)
                                            .FitArea();
                                    });
                            }
                        });
                }
            });
        }

        // =========================================================
        // BROŞÜR
        // =========================================================

        private static bool IsBrochureImage(
            SalesOrderFormImage image)
        {
            if (image.ImageTypeId == 1)
                return true;

            var code =
                image.ImageTypeCode?
                    .ToUpperInvariant();

            return code == "BROCHURE_FRONT" ||
                   code == "BROCHURE_BACK" ||
                   code == "BROCHURE";
        }

        // =========================================================
        // DOSYA YOLU
        // =========================================================

        private string GetPhysicalFilePath(
            string relativePath)
        {
            var normalizedPath =
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            return Path.Combine(
                _rootPath,
                normalizedPath);
        }

        // =========================================================
        // KOLİ
        // =========================================================

        private static string FormatKoli(
            SalesOrderFormPackage package)
        {
            var durum =
                string.IsNullOrWhiteSpace(
                    package.KoliDurum)
                    ? "-"
                    : package.KoliDurum;

            var koliIciMiktar =
                package.KoliIciMiktar > 0
                    ? $"{package.KoliIciMiktar}'li"
                    : "-";

            return $"{durum} - {koliIciMiktar}";
        }

        // =========================================================
        // RENK
        // =========================================================

        private static string FormatColors(
            SalesOrderFormCKDetails? ck)
        {
            if (ck == null)
                return "-";

            var ayak =
                FormatRal(
                    ck.AyakRal,
                    ck.AyakRalDetay);

            var govdeKanat =
                FormatRal(
                    ck.GovdeKanatRal,
                    ck.GovdeKanatRalDetay);

            return $"{ayak} / {govdeKanat}";
        }

        // =========================================================
        // PLASTİK
        // =========================================================

        private static string FormatPlastics(
            SalesOrderFormCKDetails? ck)
        {
            if (ck == null)
                return "-";

            var plastik1 =
                FormatPlastic(
                    ck.PlastikRenk1No,
                    ck.PlastikRenk1);

            var plastik2 =
                FormatPlastic(
                    ck.PlastikRenk2No,
                    ck.PlastikRenk2);

            return $"{plastik1} / {plastik2}";
        }

        // =========================================================
        // GÖRSEL KONTROLÜ
        // =========================================================

        private static bool IsImageFile(
            SalesOrderFormImage image)
        {
            var extension =
                Path.GetExtension(
                    image.FileName)
                ?.ToLowerInvariant();

            return extension == ".jpg" ||
                   extension == ".jpeg" ||
                   extension == ".png" ||
                   extension == ".bmp" ||
                   extension == ".gif" ||
                   extension == ".webp";
        }

        // =========================================================
        // RAL
        // =========================================================

        private static string FormatRal(
            string? code,
            string? name)
        {
            if (string.IsNullOrWhiteSpace(code))
                return name ?? "-";

            if (string.IsNullOrWhiteSpace(name))
                return code;

            return $"{code} - {name}";
        }

        // =========================================================
        // PLASTİK
        // =========================================================

        private static string FormatPlastic(
            string? code,
            string? name)
        {
            if (string.IsNullOrWhiteSpace(code))
                return name ?? "-";

            if (string.IsNullOrWhiteSpace(name))
                return code;

            return $"{code} - {name}";
        }

        // =========================================================
        // TABLE HEADER
        // =========================================================

        private static void HeaderCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Border(0.5f)
                .Background(
                    Colors.Grey.Lighten2)
                .Padding(4)
                .AlignMiddle()
                .Text(text)
                .Bold()
                .FontSize(7.5f);
        }

        // =========================================================
        // TABLE BODY
        // =========================================================

        private static void BodyCell(
            TableDescriptor table,
            string? text)
        {
            table.Cell()
                .Border(0.5f)
                .Padding(4)
                .AlignMiddle()
                .Text(text ?? "-")
                .FontSize(7.5f);
        }

        // =========================================================
        // INFO HEADER
        // =========================================================

        private static void InfoHeaderCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Border(0.5f)
                .Background(
                    Colors.Grey.Lighten4)
                .Padding(5)
                .AlignMiddle()
                .Text(text)
                .Bold()
                .FontSize(7.5f);
        }

        // =========================================================
        // INFO VALUE
        // =========================================================

        private static void InfoValueCell(
            TableDescriptor table,
            string? text)
        {
            table.Cell()
                .Border(0.5f)
                .Padding(5)
                .AlignMiddle()
                .Text(text ?? "-")
                .FontSize(8);
        }
    }
}