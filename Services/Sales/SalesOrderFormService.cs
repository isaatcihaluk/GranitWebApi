using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderFormService : ISalesOrderFormService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public SalesOrderFormService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<SalesOrderForm?> GetOrderFormDataAsync(long salesOrderId)
        {
            // SİPARİŞ
            var order = await _context.SalesOrders.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(x => x.CKConfiguration)
                .Include(x => x.Lines).ThenInclude(x => x.UMConfiguration)
                .Include(x => x.Lines).ThenInclude(x => x.Images!).ThenInclude(x => x.ImageType)
                .Include(x => x.Packages.Where(p => p.Status != "IPTAL")).ThenInclude(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (order == null)
                return null;

            // SATIŞ TEMSİLCİSİ
            var salesRepresentativeName = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == order.SalesRepresentativeUserId)
                .Select(x => ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim())
                .FirstOrDefaultAsync();

            // ---------------------------------------------------------
            // FORM ÜST BİLGİLERİ
            // ---------------------------------------------------------

            var result = new SalesOrderForm
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderYear = order.OrderYear,
                SystemOrderNumber = order.SystemOrderNumber,
                SalesType = order.SalesType,
                OrderType = order.OrderType,
                CustomerCode = order.CustomerCode,
                CustomerName = order.CustomerName,
                DeliveryMethod = order.DeliveryMethod,
                IncotermId = order.IncotermId,
                Incoterm = GetIncotermText(order.IncotermId),
                DueDate = order.DueDate,
                CurrencyCode = order.CurrencyCode,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Definition = order.Definition,
                SalesRepresentativeName = salesRepresentativeName
            };

            // ---------------------------------------------------------
            // AKTİF SİPARİŞ KALEMLERİ
            // ---------------------------------------------------------

            var activeLines = order.Lines
                .Where(x => !x.DeletedFlag)
                .OrderBy(x => x.LineNumber)
                .ToList();

            // ---------------------------------------------------------
            // PAKETLER
            // ---------------------------------------------------------

            result.Packages = new List<SalesOrderFormPackage>();

            foreach (var package in order.Packages
                .Where(x => x.Status != "IPTAL")
                .OrderBy(x => x.PackageNumber))
            {
                // -----------------------------------------------------
                // PAKETE DAHİL OLAN SİPARİŞ KALEMLERİ
                // -----------------------------------------------------

                var packageLines = package.Lines
                    .Select(packageLine =>
                        activeLines.FirstOrDefault(line =>
                            line.Id == packageLine.SalesOrderLineId))
                    .Where(line => line != null)
                    .ToList();

                // -----------------------------------------------------
                // KOLİ DURUMU
                // -----------------------------------------------------

                var koliDurum = await _context.ProductBoxes
                    .AsNoTracking()
                    .Where(x => x.KoliKod == package.KoliKod)
                    .Select(x => x.Durum)
                    .FirstOrDefaultAsync();

                // -----------------------------------------------------
                // PAKET MODELİ
                // -----------------------------------------------------

                var formPackage = new SalesOrderFormPackage
                {
                    Id = package.Id,
                    PackageNumber = package.PackageNumber,

                    KoliId = package.KoliId,
                    KoliKod = package.KoliKod,
                    KoliIciMiktar = package.KoliIciMiktar,
                    KoliDurum = koliDurum,

                    NetsisPaketKodu = package.NetsisPaketKodu,
                    NetsisPaketAdi = package.NetsisPaketAdi,
                    Referans = package.Referans,

                    // Paket miktarı doğrudan SalesOrderPackages'tan
                    Miktar = package.PackageQuantity ?? 0,

                    // Paket bilgileri doğrudan SalesOrderPackages'tan
                    UnitPrice = package.UnitPrice,
                    PackageQuantity = package.PackageQuantity,
                    KoliAdedi = package.KoliAdedi,
                    UnitKoliPrice = package.UnitKoliPrice,
                    TotalPrice = package.TotalPrice,

                    Definition = package.Definition,

                    Lines = new List<SalesOrderFormPackageLine>()
                };

                // -----------------------------------------------------
                // PAKET İÇERİĞİ
                // -----------------------------------------------------

                foreach (var packageLine in package.Lines)
                {
                    var orderLine = activeLines
                        .FirstOrDefault(x =>
                            x.Id == packageLine.SalesOrderLineId);

                    if (orderLine == null)
                        continue;

                    var formPackageLine = new SalesOrderFormPackageLine
                    {
                        SalesOrderLineId = orderLine.Id,
                        Quantity = packageLine.Quantity,
                        ProductName = orderLine.ProductName,
                        CatalogCode = orderLine.CatalogCode,
                        ShrinkliKod = orderLine.ShrinkliKod,
                        ShrinkliAd = orderLine.ShrinkliAd,
                        Images = new List<SalesOrderFormImage>()
                    };

                    // KULLANICI TARAFINDAN YÜKLENEN GÖRSELLER
                    formPackageLine.Images =
                        (orderLine.Images ??
                         new List<SalesOrderLineImage>())
                        .Where(x => x.IsActive)
                        .OrderBy(x =>
                            x.ImageType != null
                                ? x.ImageType.DisplayOrder
                                : int.MaxValue)
                        .Select(x => new SalesOrderFormImage
                        {
                            Id = x.Id,
                            SalesOrderLineId = x.SalesOrderLineId,
                            ImageTypeId = x.ImageTypeId,
                            ImageTypeCode = x.ImageType?.Code,
                            ImageTypeName = x.ImageType?.Name,
                            FileName = x.FileName,
                            VersionNo = x.VersionNo,
                            FilePath = x.FilePath
                        })
                        .ToList();

                    formPackage.Lines.Add(formPackageLine);
                }

                // =====================================================
                // CK DETAYLARI
                // =====================================================

                if (packageLines.Count > 0)
                {
                    var ckLine = packageLines
                        .FirstOrDefault(x =>
                            x!.ProductGroupId == "1" &&
                            x.CKConfiguration != null);

                    if (ckLine != null && ckLine.CKConfiguration != null)
                    {
                        var ck = ckLine.CKConfiguration;

                        // -------------------------------------------------
                        // PAKET KODUNUN İLK 5 HANESİ
                        // -------------------------------------------------

                        var paketKatalogKodu = package.NetsisPaketKodu;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            paketKatalogKodu =
                                paketKatalogKodu.Length >= 5
                                    ? paketKatalogKodu.Substring(0, 5)
                                    : paketKatalogKodu;
                        }

                        // -------------------------------------------------
                        // CK ANA ÜRÜN DETAYI
                        // -------------------------------------------------

                        CKProductDetailMain? ckMain = null;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            ckMain = await _context.CKProductDetailMains
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.KatalogKodu == paketKatalogKodu);
                        }

                        // -------------------------------------------------
                        // RENK DETAYLARI
                        // GRANIT_TBL_CK_URUN_DETAY_RENK
                        // -------------------------------------------------

                        CKProductDetailColor? ckColor = null;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            ckColor = await _context.CKProductDetailColors
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.KatalogKodu == paketKatalogKodu &&
                                    x.UrunTipiId == ck.ProductTypeId &&
                                    x.UrunId == ck.ProductId &&
                                    x.AyakRal == ck.FootRal &&
                                    x.GovdeKanatRal == ck.BodyWingRal &&
                                    x.PlastikRenk1No == ck.PlasticColor1No &&
                                    x.PlastikRenk2No == ck.PlasticColor2No);
                        }

                        // -------------------------------------------------
                        // PLASTİK RENKLER
                        // GRANIT_TBL_PLASTIK_RENK
                        // -------------------------------------------------

                        PlasticColor? plasticColor1 = null;
                        PlasticColor? plasticColor2 = null;

                        var plasticColorNos = new[]
                        {
                    ck.PlasticColor1No,
                    ck.PlasticColor2No
                }
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                        if (plasticColorNos.Count > 0)
                        {
                            var plasticColors = await _context.PlasticColors
                                .AsNoTracking()
                                .Where(x => plasticColorNos.Contains(x.No))
                                .ToListAsync();

                            plasticColor1 = plasticColors
                                .FirstOrDefault(x =>
                                    x.No == ck.PlasticColor1No);

                            plasticColor2 = plasticColors
                                .FirstOrDefault(x =>
                                    x.No == ck.PlasticColor2No);
                        }

                        // -------------------------------------------------
                        // KULLANILACAK SHRINK
                        // GRANIT_TBL_URUN_SHRINK
                        // -------------------------------------------------

                        ProductShrink? shrink = null;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            shrink = await _context.ProductShrinks
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.KatalogKod == paketKatalogKodu);
                        }

                        // -------------------------------------------------
                        // BROŞÜR GÖRSELLERİ
                        // -------------------------------------------------

                        var brosurImages = new List<SalesOrderFormImage>();

                        foreach (var line in packageLines)
                        {
                            if (line?.ProductGroupId != "1")
                                continue;

                            var images = (line.Images ??
                                          new List<SalesOrderLineImage>())
                                .Where(x => x.IsActive)
                                .Where(x =>
                                    x.ImageType != null &&
                                    (
                                        x.ImageType.Code == "BROCHURE_FRONT" ||
                                        x.ImageType.Code == "BROCHURE_BACK" ||
                                        x.ImageType.Code == "BROCHURE"
                                    ))
                                .OrderBy(x =>
                                    x.ImageType != null
                                        ? x.ImageType.DisplayOrder
                                        : int.MaxValue)
                                .Select(x => new SalesOrderFormImage
                                {
                                    Id = x.Id,
                                    SalesOrderLineId = x.SalesOrderLineId,
                                    ImageTypeId = x.ImageTypeId,
                                    ImageTypeCode = x.ImageType?.Code,
                                    ImageTypeName = x.ImageType?.Name,
                                    FileName = x.FileName,
                                    VersionNo = x.VersionNo,
                                    FilePath = x.FilePath
                                })
                                .ToList();

                            brosurImages.AddRange(images);
                        }

                        // -------------------------------------------------
                        // CK DETAILS
                        // -------------------------------------------------

                        formPackage.CKDetails =
                            new SalesOrderFormCKDetails
                            {
                                GovdeDetay = ckMain?.GovdeDetay,
                                KanatDetay = ckMain?.KanatDetay,

                                AyakRal = ckColor?.AyakRal,
                                AyakRalDetay = ckColor?.AyakRalDetay,

                                GovdeKanatRal = ckColor?.GovdeKanatRal,
                                GovdeKanatRalDetay =
                                    ckColor?.GovdeKanatRalDetay,

                                PlastikRenk1No = ck.PlasticColor1No,
                                PlastikRenk1 = plasticColor1?.Renk,

                                PlastikRenk2No = ck.PlasticColor2No,
                                PlastikRenk2 = plasticColor2?.Renk,

                                // SHRINK
                                ShrinkKod = shrink?.ShrinkKod,
                                ShrinkUrunAdi = shrink?.UrunAdi,
                                ShrinkDurum = shrink?.Durum,
                                ShrinkMiktar = shrink?.Miktar,

                                BrosurImages = brosurImages
                            };
                    }
                }

                // =====================================================
                // UM DETAYLARI
                // =====================================================

                if (packageLines.Count > 0)
                {
                    var umLine = packageLines
                        .FirstOrDefault(x =>
                            x!.ProductGroupId == "2" &&
                            x.UMConfiguration != null);

                    if (umLine != null && umLine.UMConfiguration != null)
                    {
                        var um = umLine.UMConfiguration;

                        // -------------------------------------------------
                        // UM PAKET KATALOG KODU
                        //
                        // CK'da olduğu gibi Netsis paket kodunun
                        // ilk 5 hanesi katalog kodudur.
                        // -------------------------------------------------

                        var paketKatalogKodu = package.NetsisPaketKodu;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            paketKatalogKodu =
                                paketKatalogKodu.Length >= 5
                                    ? paketKatalogKodu.Substring(0, 5)
                                    : paketKatalogKodu;
                        }

                        // -------------------------------------------------
                        // UM ANA ÜRÜN DETAYI
                        //
                        // GRANIT_TBL_UM_URUN_DETAY_ANA
                        // KATALOG_kodu -> CatalogCode
                        // ANTENLI = 1 -> VAR
                        // -------------------------------------------------

                        int? antenli = null;

                        if (!string.IsNullOrWhiteSpace(umLine.CatalogCode))
                        {
                            antenli = await _context.Database
                                .SqlQuery<int?>($"""
                            SELECT TOP 1
                                TRY_CONVERT(int, ANTENLI) AS Value
                            FROM GRANIT_TBL_UM_URUN_DETAY_ANA
                            WHERE KATALOG_kodu = {umLine.CatalogCode}
                            """)
                                .FirstOrDefaultAsync();
                        }

                        // -------------------------------------------------
                        // KULLANILACAK SHRINK
                        // CK ile aynı mantık
                        // GRANIT_TBL_URUN_SHRINK
                        // -------------------------------------------------

                        ProductShrink? shrink = null;

                        if (!string.IsNullOrWhiteSpace(paketKatalogKodu))
                        {
                            shrink = await _context.ProductShrinks
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.KatalogKod == paketKatalogKodu);
                        }

                        // -------------------------------------------------
                        // BROŞÜR GÖRSELLERİ
                        // -------------------------------------------------

                        var brosurImages = new List<SalesOrderFormImage>();

                        foreach (var line in packageLines)
                        {
                            if (line?.ProductGroupId != "2")
                                continue;

                            var images = (line.Images ??
                                          new List<SalesOrderLineImage>())
                                .Where(x => x.IsActive)
                                .Where(x =>
                                    x.ImageType != null &&
                                    (
                                        x.ImageType.Code == "BROCHURE_FRONT" ||
                                        x.ImageType.Code == "BROCHURE_BACK" ||
                                        x.ImageType.Code == "BROCHURE"
                                    ))
                                .OrderBy(x =>
                                    x.ImageType != null
                                        ? x.ImageType.DisplayOrder
                                        : int.MaxValue)
                                .Select(x => new SalesOrderFormImage
                                {
                                    Id = x.Id,
                                    SalesOrderLineId = x.SalesOrderLineId,
                                    ImageTypeId = x.ImageTypeId,
                                    ImageTypeCode = x.ImageType?.Code,
                                    ImageTypeName = x.ImageType?.Name,
                                    FileName = x.FileName,
                                    VersionNo = x.VersionNo,
                                    FilePath = x.FilePath
                                })
                                .ToList();

                            brosurImages.AddRange(images);
                        }

                        // -------------------------------------------------
                        // UM DETAILS
                        // -------------------------------------------------

                        formPackage.UMDetails =
                            new SalesOrderFormUMDetails
                            {
                                Govde = um.BodyCode,

                                Utuuluk = um.IroningBoardCode,

                                Ayak = um.FootCode,

                                Fis = um.HasFis
                                    ? (
                                        !string.IsNullOrWhiteSpace(um.FisName)
                                            ? um.FisName
                                            : um.FisCode
                                      )
                                    : "YOK",

                                Anten = antenli == 1
                                    ? "VAR"
                                    : "YOK",

                                Boya =
                                    $"{um.BodyIroningRal} {um.BodyIroningColor}"
                                        .Trim(),

                                PlastikRenk =
                                    um.PlasticCombinationDescription,

                                Kumas =
                                    um.FabricName,

                                Sunger =
                                    $"{um.SpongeName} ({um.SpongeQuantity:0.###})",

                                BrosurImages =
                                    brosurImages,

                                // Daha önce oluşturulan SalesOrderLine shrink
                                ShrinkKod =
                                    umLine.ShrinkliKod,

                                ShrinkAd =
                                    umLine.ShrinkliAd,

                                // ProductShrink tablosundan CK ile aynı şekilde
                                ShrinkDurum =
                                    shrink?.Durum,

                                ShrinkMiktar =
                                    shrink?.Miktar
                            };
                    }
                }

                // ---------------------------------------------------------
                // PAKETİ SONUÇ LİSTESİNE EKLE
                // ---------------------------------------------------------

                result.Packages.Add(formPackage);
            }

            return result;
        }
        private static string? GetIncotermText(int? incotermId)
        {
            return incotermId switch
            {
                1 => "FOB",
                2 => "CIF",
                3 => "CF",
                4 => "FOT",
                5 => "İhr.Kayıt",
                6 => "DAF",
                7 => "EXW",
                8 => "İhr.KurFarkı",
                9 => "CIP",
                10 => "CPT",
                11 => "DAT",
                12 => "DAP",
                13 => "DDP",
                14 => "DES",
                15 => "DEQ",
                16 => "DDU",
                17 => "FCA",
                18 => "FAS",
                19 => "CFR",
                20 => "DPU",
                _ => null
            };
        }
        public async Task<byte[]> GenerateOrderFormPdfAsync(long salesOrderId)
        {
            var order = await GetOrderFormDataAsync(salesOrderId);

            if (order == null)
                throw new Exception("Sipariş bulunamadı.");

            var rootPath = _configuration["FileStorage:RootPath"];

            if (string.IsNullOrWhiteSpace(rootPath))
                throw new Exception(
                    "FileStorage:RootPath ayarı bulunamadı.");

            // =========================================================
            // PDF KLASÖRÜ
            // =========================================================

            var directoryPath = Path.Combine(
                rootPath,
                "SalesOrders",
                order.Id.ToString(),
                "OrderForm"
            );

            Directory.CreateDirectory(directoryPath);

            // =========================================================
            // DOSYA ADI
            // =========================================================

            var fileName =
                $"{order.SystemOrderNumber ?? order.OrderNumber ?? order.Id.ToString()}.pdf";

            var physicalFilePath = Path.Combine(
                directoryPath,
                fileName
            );

            // =========================================================
            // PDF OLUŞTUR
            // =========================================================

            var document = new SalesOrderFormDocument(
                order,
                rootPath
            );

            document.GeneratePdf(physicalFilePath);

            // =========================================================
            // OLUŞAN DOSYAYI OKU
            // =========================================================

            if (!File.Exists(physicalFilePath))
                throw new Exception("Sipariş formu PDF dosyası oluşturulamadı.");

            return await File.ReadAllBytesAsync(
                physicalFilePath);
        }
    }
}