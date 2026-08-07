using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GranitWebApi.Services
{
    public class PdfService
    {
        private readonly IConfiguration _configuration;

        public PdfService(IConfiguration configuration)
        {
            _configuration = configuration;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string CreateIzinPdf(
            int requestId,
            string sicil,
            string personelAdi,
            string izinTuru,
            DateTime baslangic,
            DateTime bitis)
        {
            // Klasör yapısı: Files/{sicil}/IzinTalepleri/
            var rootPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Files",
                sicil,
                "IzinTalepleri"
            );

            Directory.CreateDirectory(rootPath);

            var fileName = $"IzinTalep_{requestId}_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = Path.Combine(rootPath, fileName);

            int toplamGun = (int)(bitis - baslangic).TotalDays + 1;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // HEADER
                        col.Item().AlignCenter().Text("İZİN TALEP FORMU").Bold().FontSize(16);

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // PERSONELİN
                        col.Item().Text("PERSONELİN").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            void Row(string label, string value)
                            {
                                table.Cell().Border(1).Padding(5).Text(label);
                                table.Cell().Border(1).Padding(5).Text(value);
                            }

                            Row("Ad Soyad", personelAdi);
                            Row("Sicil", sicil);
                            Row("Bölümü", "Yazılım");
                            Row("Görevi", "Yazılım Uzmanı");
                        });

                        // İZİN BİLGİSİ
                        col.Item().PaddingTop(15).Text("KULLANILACAK İZİN").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            void Cell(string text) =>
                                table.Cell().Border(1).Padding(5).Text(text);

                            Cell("Başlangıç Tarihi");
                            Cell(baslangic.ToString("dd.MM.yyyy"));
                            Cell("Saat");
                            Cell("15:00");

                            Cell("Bitiş Tarihi");
                            Cell(bitis.ToString("dd.MM.yyyy"));
                            Cell("Saat");
                            Cell("17:45");
                        });

                        // SÜRE
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            table.Cell().Border(1).Padding(5).Text("Toplam Süre");
                            table.Cell().Border(1).Padding(5).Text($"{toplamGun} Gün");
                        });

                        // CHECKBOX ALANI
                        col.Item().PaddingTop(15).Text("İzin Türü").Bold();

                        col.Item().Row(row =>
                        {
                            void Checkbox(string text)
                            {
                                row.RelativeItem().Row(r =>
                                {
                                    r.ConstantItem(15).Height(15).Border(1);
                                    r.RelativeItem().PaddingLeft(5).Text(text);
                                });
                            }

                            Checkbox("Yıllık İzin");
                            Checkbox("Ücretsiz İzin");
                            Checkbox("Sağlık İzni");
                            Checkbox("Diğer");
                        });

                        // İMZA
                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            void Imza(string title)
                            {
                                table.Cell().Border(1).Height(60).AlignBottom().AlignCenter().Text(title);
                            }

                            Imza("Personel İmza");
                            Imza("Yönetici Onay");
                            Imza("Genel Müdür");
                        });
                    });
                });
            }).GeneratePdf(filePath);

            //Document.Create(container =>
            //{
            //    container.Page(page =>
            //    {
            //        page.Size(PageSizes.A4);
            //        page.Margin(2, Unit.Centimetre);
            //        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

            //        page.Header().Column(col =>
            //        {
            //            col.Item().AlignCenter().Text("GRANİT METAL")
            //                .FontSize(18).Bold();
            //            col.Item().AlignCenter().Text("İZİN TALEP FORMU")
            //                .FontSize(14).Bold();
            //            col.Item().PaddingTop(5).LineHorizontal(1);
            //        });

            //        page.Content().PaddingTop(20).Column(col =>
            //        {
            //            col.Item().Table(table =>
            //            {
            //                table.ColumnsDefinition(c =>
            //                {
            //                    c.RelativeColumn(2);
            //                    c.RelativeColumn(3);
            //                });

            //                void Satir(string label, string value)
            //                {
            //                    table.Cell().Padding(6).Background("#f5f5f5").Text(label).Bold();
            //                    table.Cell().Padding(6).Text(value);
            //                }

            //                Satir("Sicil No", sicil);
            //                Satir("Ad Soyad", personelAdi);
            //                Satir("İzin Türü", izinTuru);
            //                Satir("Başlangıç Tarihi", baslangic.ToString("dd.MM.yyyy"));
            //                Satir("Bitiş Tarihi", bitis.ToString("dd.MM.yyyy"));
            //                Satir("Toplam Gün", $"{toplamGun} gün");
            //                Satir("Onay Tarihi", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            //                Satir("Durum", "ONAYLANDI");
            //            });
            //        });

            //        page.Footer().AlignCenter().Text(x =>
            //        {
            //            x.Span("Oluşturulma Tarihi: ");
            //            x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            //        });
            //    });
            //}).GeneratePdf(filePath);

            return filePath;
        }
    }
}
