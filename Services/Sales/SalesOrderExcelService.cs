using ClosedXML.Excel;
using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderExcelService
    {
        private readonly ISalesOrderFormService _salesOrderFormService;

        public SalesOrderExcelService(ISalesOrderFormService salesOrderFormService)
        {
            _salesOrderFormService = salesOrderFormService;
        }

        public async Task<byte[]> CreateSalesOrderExcelAsync(long salesOrderId)
        {
            var form = await _salesOrderFormService.GetOrderFormDataAsync(salesOrderId);
            if (form == null) throw new Exception("Sipariş bulunamadı.");

            // EXCEL
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sipariş Formu");

            // GENEL SAYFA AYARLARI
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.Style.Font.FontName = "Arial";
            ws.Style.Font.FontSize = 9;

            // =========================================================
            // SÜTUN GENİŞLİKLERİ
            // A:Q = 17 KOLON
            // =========================================================

            ws.Column("A").Width = 6;   // SIRA
            ws.Column("B").Width = 15;  // KOD / PAKET KOD
            ws.Column("C").Width = 30;  // ÜRÜN ADI / PAKET AD
            ws.Column("D").Width = 10;  // MİKTAR
            ws.Column("E").Width = 12;  // KANAT / GÖVDE
            ws.Column("F").Width = 12;  // GÖVDE / ÜTÜLÜK
            ws.Column("G").Width = 12;  // AYAK
            ws.Column("H").Width = 12;  // TEL / FİŞ
            ws.Column("I").Width = 13;  // BROŞÜR / ANTEN
            ws.Column("J").Width = 12;  // BOYA
            ws.Column("K").Width = 18;  // PLASTİK
            ws.Column("L").Width = 20;  // SHRINK / KUMAŞ
            ws.Column("M").Width = 12;  // PAKET / SÜNGER
            ws.Column("N").Width = 15;  // KOLİ / BROŞÜR
            ws.Column("O").Width = 15;  // AÇIKLAMALAR / SHRINK
            ws.Column("P").Width = 15;  // KOLİ
            ws.Column("Q").Width = 25;  // AÇIKLAMALAR

            // BAŞLIK
            ws.Range("A1:Q1").Merge();
            var title = ws.Cell("A1");
            title.Value = "SATIŞ SİPARİŞ FORMU";
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 30;
            title.Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Center;
            title.Style.Alignment.Vertical =XLAlignmentVerticalValues.Center;
            title.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
            ws.Row(1).Height = 100;

            // =========================================================
            // ÜST BİLGİLER
            // =========================================================
            int row = 2;

            AddInfoRow(ws,row++,"MÜŞTERİ ADI",form.CustomerName);
            AddInfoRow(ws,row++,"PRF NO",form.SystemOrderNumber);
            AddInfoRow(ws,row++,"SİPARİŞ TARİHİ / REVİZE TARİHİ",form.CreatedAt?.ToString("dd.MM.yyyy") +" / " +form.UpdatedAt?.ToString("dd.MM.yyyy"));
            AddInfoRow(ws,row++,"SİPARİŞ NO",form.SystemOrderNumber);
            AddInfoRow(ws,row++,"YÜKLEME ŞEKLİ",form.DeliveryMethod +" / " +form.Incoterm);
            AddInfoRow(ws,row++,"DEPO TESLİM TARİHİ",form.DueDate?.ToString("dd.MM.yyyy"));
            AddInfoRow(ws,row++,"REVİZE KONUSU","");
            AddInfoRow(ws,row++,"MÜŞTERİ TEMSİLCİSİ",form.SalesRepresentativeName);

            row += 1;

            // ÜRÜN GRUPLARINI AYIR
            var ckPackages = form.Packages.Where(x => x.CKDetails != null).ToList();
            var umPackages = form.Packages.Where(x => x.UMDetails != null).ToList();

            // KURUTMALIK - CK
            if (ckPackages.Count > 0)
            {
                row = AddCKSection(ws,row,ckPackages);
            }

            // ÜTÜ MASASI - UM
            if (umPackages.Count > 0)
            {
                row = AddUMSection(ws,row,umPackages);
            }

            // GENEL AÇIKLAMA ALANI
            row += 1;

            int explanationStartRow = row;
            int explanationEndRow = row + 4;

            ws.Range(explanationStartRow,1,explanationEndRow,17).Merge();
            var explanationCell =ws.Cell(explanationStartRow, 1);
            var explanationText = "AÇIKLAMA:";
            explanationText +="\n\n" +(form.Definition ?? "");
            explanationCell.Value =explanationText;
            explanationCell.Style.Font.FontSize = 9;
            explanationCell.Style.Alignment.Vertical =XLAlignmentVerticalValues.Top;
            explanationCell.Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Left;
            explanationCell.Style.Alignment.WrapText =true;
            explanationCell.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
            explanationCell.Style.Fill.BackgroundColor =XLColor.Yellow;
            ws.Row(explanationStartRow).Height = 25;

            // GENEL HİZALAMA / KENARLAR
            ws.Range(1,1,explanationEndRow,17).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(1,1,explanationEndRow,17).Style.Border.OutsideBorder =XLBorderStyleValues.Thin;

            // EXCEL OLUŞTUR
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // CK SECTION
        private static int AddCKSection(IXLWorksheet ws,int row,List<SalesOrderFormPackage> packages)
        {
            // ÜRÜN GRUBU BAŞLIĞI
            int groupRow = row;

            // CK sadece A:O kullanır.
            ws.Range(groupRow,1,groupRow,15).Merge();
            var groupCell = ws.Cell(groupRow, 1);
            groupCell.Value = "KURUTMALIK";
            groupCell.Style.Font.Bold = true;
            groupCell.Style.Font.FontSize = 10;
            groupCell.Style.Font.FontColor =XLColor.White;
            groupCell.Style.Fill.BackgroundColor =XLColor.FromHtml("#3668E8");
            groupCell.Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Center;
            groupCell.Style.Alignment.Vertical =XLAlignmentVerticalValues.Center;
            groupCell.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
            ws.Row(groupRow).Height = 25;
            row++;

            // CK TABLO BAŞLIKLARI
            int headerRow = row;

            string[] headersCk =
            {
                "SIRA",
                "KOD",
                "ÜRÜN ADI",
                "MİKTAR",
                "KANAT",
                "GÖVDE",
                "AYAK",
                "TEL",
                "BROŞÜR",
                "BOYA",
                "PLASTİK",
                "SHRINK",
                "PAKET",
                "KOLİ",
                "AÇIKLAMALAR"
            };

            for (int i = 0; i < headersCk.Length; i++)
            {
                ws.Cell(headerRow,i + 1).Value = headersCk[i];
            }
            StyleHeader(ws,headerRow,15);

            row++;
            int dataStartRow = row;
            int sıra = 1;

            // CK PAKETLERİ
            foreach (var package in packages)
            {
                ws.Cell(row, 1).Value =sıra++;
                ws.Cell(row, 2).Value =package.NetsisPaketKodu ?? "";
                ws.Cell(row, 3).Value =package.NetsisPaketAdi ?? "";
                ws.Cell(row, 4).Value =package.KoliAdedi;
                if (package.CKDetails != null)
                {
                    var ck =package.CKDetails;
                    ws.Cell(row, 5).Value =ck.KanatDetay ?? "";
                    ws.Cell(row, 6).Value ="";
                    ws.Cell(row, 7).Value ="";
                    ws.Cell(row, 8).Value ="Kanat: " +ck.KanatDetay +" /Ayak: " +ck.GovdeDetay;
                    ws.Cell(row, 9).Value =BuildBrochureText(ck);
                    ws.Cell(row, 10).Value =BuildPlasticText(ck.AyakRalDetay,ck.GovdeKanatRalDetay);
                    ws.Cell(row, 11).Value =BuildPlasticText(ck.PlastikRenk1,ck.PlastikRenk2);
                    ws.Cell(row, 12).Value = ck.ShrinkDurum;
                }

                ws.Cell(row, 13).Value = package.KoliIciMiktar + "'LI " + package.KoliDurum;
                ws.Cell(row, 14).Value = package.KoliKod ?? "";
                ws.Cell(row, 15).Value = package.Definition ?? "";

                var dataRange =ws.Range(row,1,row,15);
                dataRange.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder =XLBorderStyleValues.Thin;
                dataRange.Style.Alignment.Vertical =XLAlignmentVerticalValues.Center;
                dataRange.Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Center;
                dataRange.Style.Alignment.WrapText =true;
                ws.Cell(row, 3).Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Left;
                ws.Cell(row, 15).Style.Alignment.Horizontal =XLAlignmentHorizontalValues.Left;
                ws.Cell(row, 15).Style.Fill.BackgroundColor =XLColor.Yellow;
                ws.Row(row).Height = 75;
                row++;
            }

            // CK TOPLAM
            int lastDataRow = row - 1;
            if (lastDataRow >= dataStartRow)
            {
                int totalRow = row;
                ws.Range(totalRow,1,totalRow,3).Merge();
                ws.Cell(totalRow, 1).Value = "TOPLAM SİPARİŞ";
                ws.Cell(totalRow, 1) .Style.Font.Bold = true;
                ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(totalRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(totalRow, 4).SetFormulaA1($"SUM(D{dataStartRow}:D{lastDataRow})");
                ws.Cell(totalRow, 4).Style.Font.Bold = true;

                //ws.Cell(totalRow, 4).Style.NumberFormat.Format ="0.###";

                var totalRange =ws.Range(totalRow,1,totalRow,15);
                totalRange.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
                totalRange.Style.Border.InsideBorder =XLBorderStyleValues.Thin;
                totalRange.Style.Alignment.Vertical =XLAlignmentVerticalValues.Center;
                ws.Row(totalRow).Height = 22;
                row = totalRow + 1;
            }
            return row + 2;
        }

        // UM SECTION
        private static int AddUMSection(IXLWorksheet ws,int row,List<SalesOrderFormPackage> packages)
        {
            int groupRow = row;
            // UM A:Q kullanır.
            ws.Range(groupRow,1,groupRow,17).Merge();
            var groupCell = ws.Cell(groupRow, 1);
            groupCell.Value = "ÜTÜ MASASI";
            groupCell.Style.Font.Bold = true;
            groupCell.Style.Font.FontSize = 10;
            groupCell.Style.Font.FontColor = XLColor.White;
            groupCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3668E8");
            groupCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            groupCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            groupCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(groupRow).Height = 25;
            row++;

            // UM TABLO BAŞLIKLARI
            // A:Q = 17 KOLON
            int headerRow = row;
            string[] headersUm =
            {
                "SIRA",
                "PAKET KOD",
                "PAKET AD",
                "MİKTAR",
                "GÖVDE",
                "ÜTÜLÜK",
                "AYAK",
                "FİŞ",
                "ANTEN",
                "BOYA",
                "PLASTİK RENK",
                "KUMAŞ",
                "SÜNGER",
                "BROŞÜR",
                "SHRINK",
                "KOLİ",
                "AÇIKLAMALAR"
            };

            for (int i = 0; i < headersUm.Length; i++)
            {
                ws.Cell(headerRow,i + 1).Value = headersUm[i];
            }

            StyleHeader(ws,headerRow,17);
            row++;
            int dataStartRow = row;
            int sıra = 1;

            // UM PAKETLERİ
            foreach (var package in packages)
            {
                var um = package.UMDetails;
                if (um == null) continue;

                ws.Cell(row, 1).Value = sıra++;
                ws.Cell(row, 2).Value = package.NetsisPaketKodu ?? "";
                ws.Cell(row, 3).Value =package.NetsisPaketAdi ?? "";
                ws.Cell(row, 4).Value =package.KoliAdedi;
                ws.Cell(row, 5).Value =um.Govde ?? "";
                ws.Cell(row, 6).Value =um.Utuuluk ?? "";
                ws.Cell(row, 7).Value =um.Ayak ?? "";
                ws.Cell(row, 8).Value =um.Fis ?? "";
                ws.Cell(row, 9).Value =um.Anten ?? "";
                ws.Cell(row, 10).Value =um.Boya ?? "";
                ws.Cell(row, 11).Value =um.PlastikRenk ?? "";
                ws.Cell(row, 12).Value =um.Kumas ?? "";
                ws.Cell(row, 13).Value =um.Sunger ?? "";
                ws.Cell(row, 14).Value =BuildBrochureText(um.BrosurImages);
                ws.Cell(row, 15).Value = um.ShrinkDurum;
                ws.Cell(row, 16).Value = package.KoliIciMiktar + "'LI " + package.KoliDurum;
                ws.Cell(row, 17).Value = package.Definition ?? "";

                // -----------------------------------------------------
                // SATIR STİLİ
                // -----------------------------------------------------

                var dataRange =
                    ws.Range(
                        row,
                        1,
                        row,
                        17
                    );

                dataRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                dataRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

                dataRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                dataRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                dataRange.Style.Alignment.WrapText =
                    true;

                // Paket adı sola
                ws.Cell(row, 3)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Left;

                // Açıklama sola
                ws.Cell(row, 17)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Left;

                // Açıklama sarı
                ws.Cell(row, 17)
                    .Style.Fill.BackgroundColor =
                    XLColor.Yellow;

                ws.Row(row).Height = 75;

                row++;
            }

            // ---------------------------------------------------------
            // UM TOPLAM
            // ---------------------------------------------------------

            int lastDataRow = row - 1;

            if (lastDataRow >= dataStartRow)
            {
                int totalRow = row;

                // A:C birleşsin
                ws.Range(
                    totalRow,
                    1,
                    totalRow,
                    3
                ).Merge();

                ws.Cell(totalRow, 1).Value =
                    "TOPLAM SİPARİŞ";

                ws.Cell(totalRow, 1)
                    .Style.Font.Bold = true;

                ws.Cell(totalRow, 1)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Right;

                ws.Cell(totalRow, 1)
                    .Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                // Miktar D sütununda
                ws.Cell(totalRow, 4)
                    .SetFormulaA1(
                        $"SUM(D{dataStartRow}:D{lastDataRow})"
                    );

                ws.Cell(totalRow, 4)
                    .Style.Font.Bold = true;

                ws.Cell(totalRow, 4)
                    .Style.NumberFormat.Format =
                    "0.###";

                var totalRange =
                    ws.Range(
                        totalRow,
                        1,
                        totalRow,
                        17
                    );

                totalRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                totalRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

                totalRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                ws.Row(totalRow).Height = 22;

                row =
                    totalRow + 1;
            }

            return row;
        }

        // =============================================================
        // HEADER STYLE
        // =============================================================

        private static void StyleHeader(
            IXLWorksheet ws,
            int row,
            int columnCount)
        {
            var headerRange =
                ws.Range(
                    row,
                    1,
                    row,
                    columnCount
                );

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Font.FontSize = 8;

            headerRange.Style.Font.FontColor =
                XLColor.White;

            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#3668E8");

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Alignment.WrapText =
                true;

            headerRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            ws.Row(row).Height = 30;
        }

        // =============================================================
        // ÜST BİLGİ SATIRI
        // =============================================================

        private static void AddInfoRow(
            IXLWorksheet ws,
            int row,
            string label,
            string? value)
        {
            // A:C = başlık
            ws.Range(
                row,
                1,
                row,
                3
            ).Merge();

            var labelCell =
                ws.Cell(row, 1);

            labelCell.Value =
                label;

            labelCell.Style.Font.Bold =
                true;

            labelCell.Style.Font.FontSize =
                8;

            labelCell.Style.Font.FontColor =
                XLColor.FromHtml("#0000CC");

            labelCell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            labelCell.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // D:Q = değer
            ws.Range(
                row,
                4,
                row,
                17
            ).Merge();

            var valueCell =
                ws.Cell(row, 4);

            valueCell.Value =
                value ?? "";

            valueCell.Style.Font.FontSize =
                9;

            valueCell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            valueCell.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Kenarlık
            var range =
                ws.Range(
                    row,
                    1,
                    row,
                    17
                );

            range.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            ws.Row(row).Height = 20;
        }

        // =============================================================
        // PLASTİK
        // =============================================================

        private static string BuildPlasticText(
            string? renk1,
            string? renk2)
        {
            var values =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(renk1))
            {
                values.Add(
                    renk1.Trim()
                );
            }

            if (!string.IsNullOrWhiteSpace(renk2) &&
                !string.Equals(
                    renk2.Trim(),
                    renk1?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                values.Add(
                    renk2.Trim()
                );
            }

            return string.Join(
                " / ",
                values
            );
        }

        // =============================================================
        // SHRINK
        // =============================================================

        private static string BuildShrinkText(
            string? kod,
            string? ad)
        {
            bool hasKod =
                !string.IsNullOrWhiteSpace(kod);

            bool hasAd =
                !string.IsNullOrWhiteSpace(ad);

            if (hasKod && hasAd)
            {
                return
                    $"{kod!.Trim()}\n{ad!.Trim()}";
            }

            if (hasKod)
                return kod!.Trim();

            if (hasAd)
                return ad!.Trim();

            return "";
        }

        // =============================================================
        // CK BROŞÜR
        // =============================================================

        private static string BuildBrochureText(
            SalesOrderFormCKDetails ck)
        {
            if (ck.BrosurImages == null ||
                ck.BrosurImages.Count == 0)
            {
                return "";
            }

            var names =
                ck.BrosurImages
                    .Select(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ImageTypeName)
                            ? x.ImageTypeName
                            : x.FileName)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                        x!.Trim())
                    .Distinct()
                    .ToList();

            return string.Join(
                "\n",
                names
            );
        }

        // =============================================================
        // UM BROŞÜR
        // =============================================================

        private static string BuildBrochureText(
            List<SalesOrderFormImage>? images)
        {
            if (images == null ||
                images.Count == 0)
            {
                return "";
            }

            var names =
                images
                    .Select(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ImageTypeName)
                            ? x.ImageTypeName
                            : x.FileName)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                        x!.Trim())
                    .Distinct()
                    .ToList();

            return string.Join(
                "\n",
                names
            );
        }
    }
}
