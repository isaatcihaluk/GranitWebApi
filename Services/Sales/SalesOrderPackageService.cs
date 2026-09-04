using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderPackageService : ISalesOrderPackageService
    {
        private readonly AppDbContext _context;

        private class PackageInfo
        {
            public string Code { get; set; } = null!;
            public string Name { get; set; } = null!;
        }

        public SalesOrderPackageService(AppDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // PAKETLEME VERİLERİNİ GETİR
        // =========================================================

        public async Task<SalesOrder?> GetPackagingDataAsync(long salesOrderId)
        {
            var order = await _context.SalesOrders
                .AsNoTracking()

                .Include(x => x.Lines)
                    .ThenInclude(x => x.CKConfiguration)

                .Include(x => x.Lines)
                    .ThenInclude(x => x.PackageLines)

                .Include(x => x.Packages)
                    .ThenInclude(x => x.Lines)

                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            return order;
        }


        // =========================================================
        // PAKET OLUŞTUR
        // =========================================================

        public async Task<SalesOrderPackage> CreatePackageAsync(
            SalesOrderPackageCreateRequest request,
            int userId)
        {
            // ---------------------------------------------------------
            // 1. SİPARİŞ
            // ---------------------------------------------------------

            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SalesOrderId);

            if (order == null)
            {
                throw new Exception(
                    "Sipariş bulunamadı.");
            }


            // ---------------------------------------------------------
            // 2. SİPARİŞ DURUMU
            // ---------------------------------------------------------

            if (order.Status != "PAKETLEME")
            {
                throw new Exception(
                    "Bu sipariş paketleme aşamasında değil.");
            }


            // ---------------------------------------------------------
            // 3. REFERANS
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.Referans))
            {
                throw new Exception(
                    "Paket referansı girilmelidir.");
            }

            var referans =
                request.Referans.Trim();


            // ---------------------------------------------------------
            // 4. PAKET KALEMLERİ
            // ---------------------------------------------------------

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new Exception(
                    "Pakete en az bir ürün eklenmelidir.");
            }


            // ---------------------------------------------------------
            // 5. AYNI KALEM İKİ KEZ EKLENEMEZ
            // ---------------------------------------------------------

            var duplicateLine =
                request.Lines
                    .GroupBy(x => x.SalesOrderLineId)
                    .FirstOrDefault(x => x.Count() > 1);

            if (duplicateLine != null)
            {
                throw new Exception(
                    $"Sipariş kalemi {duplicateLine.Key} " +
                    $"pakete birden fazla eklenemez.");
            }


            // ---------------------------------------------------------
            // 6. KOLİ MASTER
            // ---------------------------------------------------------

            var koli = await _context.ProductBoxes
                .FirstOrDefaultAsync(x =>
                    x.Id == request.KoliId);

            if (koli == null)
            {
                throw new Exception(
                    "Seçilen koli bulunamadı.");
            }

            if (!koli.KoliIciMiktar.HasValue ||
                koli.KoliIciMiktar.Value <= 0)
            {
                throw new Exception(
                    "Seçilen kolinin iç miktarı geçersiz.");
            }

            if (string.IsNullOrWhiteSpace(koli.KoliKod))
            {
                throw new Exception(
                    "Seçilen kolinin koli kodu bulunamadı.");
            }

            var koliIciMiktar =
                koli.KoliIciMiktar.Value;


            // ---------------------------------------------------------
            // 7. SİPARİŞ KALEMLERİ
            // ---------------------------------------------------------

            var lineIds =
                request.Lines
                    .Select(x => x.SalesOrderLineId)
                    .ToList();

            var orderLines =
                await _context.SalesOrderLines
                    .Include(x => x.CKConfiguration)
                    .Where(x =>
                        x.SalesOrderId ==
                            request.SalesOrderId &&
                        lineIds.Contains(x.Id) &&
                        !x.DeletedFlag)
                    .ToListAsync();


            // ---------------------------------------------------------
            // 8. SİPARİŞ KALEMİ KONTROLÜ
            // ---------------------------------------------------------

            if (orderLines.Count != lineIds.Count)
            {
                throw new Exception(
                    "Pakete eklenen sipariş kalemlerinden biri " +
                    "bu siparişe ait değil.");
            }


            // ---------------------------------------------------------
            // 9. SHRINKLİ KOD
            // ---------------------------------------------------------

            foreach (var requestLine in request.Lines)
            {
                var orderLine =
                    orderLines.First(x =>
                        x.Id == requestLine.SalesOrderLineId);

                if (string.IsNullOrWhiteSpace(
                    orderLine.ShrinkliKod))
                {
                    throw new Exception(
                        $"{orderLine.ProductName} için " +
                        "shrinkli ürün kodu oluşturulmamış.");
                }
            }


            // ---------------------------------------------------------
            // 10. MİKTAR KONTROLÜ
            // ---------------------------------------------------------

            foreach (var requestLine in request.Lines)
            {
                if (requestLine.Quantity <= 0)
                {
                    throw new Exception(
                        $"Sipariş kalemi " +
                        $"{requestLine.SalesOrderLineId} için " +
                        "miktar 0'dan büyük olmalıdır.");
                }
            }


            // ---------------------------------------------------------
            // 11. KOLİ KAPASİTESİ
            // ---------------------------------------------------------

            var totalPackageQuantity =
                request.Lines.Sum(x => x.Quantity);

            if (totalPackageQuantity != koliIciMiktar)
            {
                throw new Exception(
                    $"Pakete eklenen toplam miktar " +
                    $"{totalPackageQuantity} olmalıdır. " +
                    $"Seçilen kolinin kapasitesi " +
                    $"{koliIciMiktar}.");
            }


            // ---------------------------------------------------------
            // 12. PAKET NUMARASI
            // ---------------------------------------------------------

            var lastPackageNumber =
                await _context.SalesOrderPackages
                    .Where(x =>
                        x.SalesOrderId ==
                        request.SalesOrderId)
                    .Select(x =>
                        (int?)x.PackageNumber)
                    .MaxAsync() ?? 0;

            var newPackageNumber =
                lastPackageNumber + 1;


            // ---------------------------------------------------------
            // 13. PAKET OLUŞTUR
            // ---------------------------------------------------------

            var now = DateTime.Now;

            var package =
                new SalesOrderPackage
                {
                    SalesOrderId =
                        request.SalesOrderId,

                    PackageNumber =
                        newPackageNumber,

                    KoliId =
                        koli.Id,

                    KoliKod =
                        koli.KoliKod,

                    KoliIciMiktar =
                        koliIciMiktar,

                    Referans =
                        referans,

                    Status =
                        "TASLAK",

                    CreatedAt =
                        now,

                    CreatedBy =
                        userId
                };


            // ---------------------------------------------------------
            // 14. PAKET KALEMLERİ
            // ---------------------------------------------------------

            foreach (var requestLine in request.Lines)
            {
                package.Lines.Add(
                    new SalesOrderPackageLine
                    {
                        SalesOrderLineId =
                            requestLine.SalesOrderLineId,

                        Quantity =
                            requestLine.Quantity,

                        CreatedAt =
                            now,

                        CreatedBy =
                            userId
                    });
            }


            // ---------------------------------------------------------
            // 15. NETSİS PAKET KODU / ADI
            // ---------------------------------------------------------

            var packageInfo =
                await GenerateCkPackageInfoAsync(
                    package,
                    orderLines);

            package.NetsisPaketKodu =
                packageInfo.Code;

            package.NetsisPaketAdi =
                packageInfo.Name;


            // ---------------------------------------------------------
            // 16. KAYDET
            // ---------------------------------------------------------

            _context.SalesOrderPackages.Add(package);

            await _context.SaveChangesAsync();


            // ---------------------------------------------------------
            // 17. DÖNDÜR
            // ---------------------------------------------------------

            return package;
        }


        // =========================================================
        // PAKET GÜNCELLE
        // =========================================================

        public async Task<SalesOrderPackage> UpdatePackageAsync(
            long packageId,
            SalesOrderPackageCreateRequest request,
            int userId)
        {
            // ---------------------------------------------------------
            // 1. SİPARİŞ
            // ---------------------------------------------------------

            var order =
                await _context.SalesOrders
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.SalesOrderId);

            if (order == null)
            {
                throw new Exception(
                    "Sipariş bulunamadı.");
            }


            // ---------------------------------------------------------
            // 2. SİPARİŞ DURUMU
            // ---------------------------------------------------------

            if (order.Status != "PAKETLEME")
            {
                throw new Exception(
                    "Bu sipariş paketleme aşamasında değil.");
            }


            // ---------------------------------------------------------
            // 3. REFERANS
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.Referans))
            {
                throw new Exception(
                    "Paket referansı girilmelidir.");
            }

            var referans =
                request.Referans.Trim();


            // ---------------------------------------------------------
            // 4. PAKET
            // ---------------------------------------------------------

            var package =
                await _context.SalesOrderPackages
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.Id == packageId &&
                        x.SalesOrderId ==
                            request.SalesOrderId);

            if (package == null)
            {
                throw new Exception(
                    "Paket bulunamadı.");
            }


            // ---------------------------------------------------------
            // 5. PAKET DURUMU
            // ---------------------------------------------------------

            if (package.Status != "TASLAK")
            {
                throw new Exception(
                    "Sadece TASLAK durumundaki paketler " +
                    "güncellenebilir.");
            }


            // ---------------------------------------------------------
            // 6. PAKET KALEMLERİ
            // ---------------------------------------------------------

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new Exception(
                    "Pakete en az bir ürün eklenmelidir.");
            }


            // ---------------------------------------------------------
            // 7. AYNI KALEM İKİ KEZ EKLENEMEZ
            // ---------------------------------------------------------

            var duplicateLine =
                request.Lines
                    .GroupBy(x => x.SalesOrderLineId)
                    .FirstOrDefault(x =>
                        x.Count() > 1);

            if (duplicateLine != null)
            {
                throw new Exception(
                    $"Sipariş kalemi {duplicateLine.Key} " +
                    $"pakete birden fazla eklenemez.");
            }


            // ---------------------------------------------------------
            // 8. KOLİ MASTER
            // ---------------------------------------------------------

            var koli =
                await _context.ProductBoxes
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.KoliId);

            if (koli == null)
            {
                throw new Exception(
                    "Seçilen koli bulunamadı.");
            }

            if (!koli.KoliIciMiktar.HasValue ||
                koli.KoliIciMiktar.Value <= 0)
            {
                throw new Exception(
                    "Seçilen kolinin iç miktarı geçersiz.");
            }

            if (string.IsNullOrWhiteSpace(
                koli.KoliKod))
            {
                throw new Exception(
                    "Seçilen kolinin koli kodu bulunamadı.");
            }

            var koliIciMiktar =
                koli.KoliIciMiktar.Value;


            // ---------------------------------------------------------
            // 9. SİPARİŞ KALEMLERİ
            // ---------------------------------------------------------

            var lineIds =
                request.Lines
                    .Select(x =>
                        x.SalesOrderLineId)
                    .ToList();

            var orderLines =
                await _context.SalesOrderLines
                    .Include(x => x.CKConfiguration)
                    .Where(x =>
                        x.SalesOrderId ==
                            request.SalesOrderId &&
                        lineIds.Contains(x.Id) &&
                        !x.DeletedFlag)
                    .ToListAsync();


            // ---------------------------------------------------------
            // 10. KALEM SİPARİŞE AİT Mİ?
            // ---------------------------------------------------------

            if (orderLines.Count != lineIds.Count)
            {
                throw new Exception(
                    "Pakete eklenen sipariş kalemlerinden biri " +
                    "bu siparişe ait değil.");
            }


            // ---------------------------------------------------------
            // 11. SHRINKLİ KOD
            // ---------------------------------------------------------

            foreach (var requestLine in request.Lines)
            {
                var orderLine =
                    orderLines.First(x =>
                        x.Id ==
                        requestLine.SalesOrderLineId);

                if (string.IsNullOrWhiteSpace(
                    orderLine.ShrinkliKod))
                {
                    throw new Exception(
                        $"{orderLine.ProductName} için " +
                        "shrinkli ürün kodu oluşturulmamış.");
                }
            }


            // ---------------------------------------------------------
            // 12. MİKTAR
            // ---------------------------------------------------------

            foreach (var requestLine in request.Lines)
            {
                if (requestLine.Quantity <= 0)
                {
                    throw new Exception(
                        $"Sipariş kalemi " +
                        $"{requestLine.SalesOrderLineId} için " +
                        "miktar 0'dan büyük olmalıdır.");
                }
            }


            // ---------------------------------------------------------
            // 13. KOLİ KAPASİTESİ
            // ---------------------------------------------------------

            var totalPackageQuantity =
                request.Lines.Sum(x =>
                    x.Quantity);

            if (totalPackageQuantity !=
                koliIciMiktar)
            {
                throw new Exception(
                    $"Pakete eklenen toplam miktar " +
                    $"{totalPackageQuantity} olmalıdır. " +
                    $"Seçilen kolinin kapasitesi " +
                    $"{koliIciMiktar}.");
            }


            // ---------------------------------------------------------
            // 14. TRANSACTION
            // ---------------------------------------------------------

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var now =
                    DateTime.Now;


                // -----------------------------------------------------
                // 15. PAKET BİLGİLERİ
                // -----------------------------------------------------

                package.KoliId =
                    koli.Id;

                package.KoliKod =
                    koli.KoliKod;

                package.KoliIciMiktar =
                    koliIciMiktar;

                package.Referans =
                    referans;

                package.UpdatedAt =
                    now;

                package.UpdatedBy =
                    userId;


                // -----------------------------------------------------
                // 16. NETSİS PAKET KODU / ADI
                //
                // Mevcut paket counter hesabına dahil edilmez.
                // -----------------------------------------------------

                var packageInfo =
                    await GenerateCkPackageInfoAsync(
                        package,
                        orderLines,
                        package.Id);

                package.NetsisPaketKodu =
                    packageInfo.Code;

                package.NetsisPaketAdi =
                    packageInfo.Name;


                // -----------------------------------------------------
                // 17. ESKİ PAKET KALEMLERİ
                // -----------------------------------------------------

                var oldPackageLines =
                    package.Lines.ToList();

                _context.SalesOrderPackageLines
                    .RemoveRange(oldPackageLines);

                package.Lines.Clear();


                // -----------------------------------------------------
                // 18. YENİ PAKET KALEMLERİ
                // -----------------------------------------------------

                foreach (var requestLine in request.Lines)
                {
                    package.Lines.Add(
                        new SalesOrderPackageLine
                        {
                            SalesOrderPackageId =
                                package.Id,

                            SalesOrderLineId =
                                requestLine.SalesOrderLineId,

                            Quantity =
                                requestLine.Quantity,

                            CreatedAt =
                                now,

                            CreatedBy =
                                userId
                        });
                }


                // -----------------------------------------------------
                // 19. KAYDET
                // -----------------------------------------------------

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // -----------------------------------------------------
                // 20. DÖNDÜR
                // -----------------------------------------------------

                return package;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =========================================================
        // PAKET İPTAL
        // =========================================================

        public async Task CancelPackageAsync(
            long packageId,
            long salesOrderId,
            int userId)
        {
            // ---------------------------------------------------------
            // 1. PAKET
            // ---------------------------------------------------------

            var package =
                await _context.SalesOrderPackages
                    .FirstOrDefaultAsync(x =>
                        x.Id == packageId &&
                        x.SalesOrderId ==
                            salesOrderId);

            if (package == null)
            {
                throw new Exception(
                    "Paket bulunamadı.");
            }


            // ---------------------------------------------------------
            // 2. PAKET DURUMU
            // ---------------------------------------------------------

            if (package.Status != "TASLAK")
            {
                throw new Exception(
                    "Sadece TASLAK durumundaki paketler " +
                    "iptal edilebilir.");
            }


            // ---------------------------------------------------------
            // 3. İPTAL
            // ---------------------------------------------------------

            package.Status =
                "IPTAL";

            package.UpdatedAt =
                DateTime.Now;

            package.UpdatedBy =
                userId;


            // ---------------------------------------------------------
            // 4. KAYDET
            // ---------------------------------------------------------

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // CK PAKET KODU / ADI OLUŞTUR
        // =========================================================

        private async Task<PackageInfo> GenerateCkPackageInfoAsync(
            SalesOrderPackage package,
            List<SalesOrderLine> orderLines,
            long? excludePackageId = null)
        {
            // ---------------------------------------------------------
            // 1. KALEM KONTROLÜ
            // ---------------------------------------------------------

            if (orderLines == null ||
                orderLines.Count == 0)
            {
                throw new Exception(
                    "Paket için sipariş kalemi bulunamadı.");
            }


            // ---------------------------------------------------------
            // 2. SADECE CK
            // ---------------------------------------------------------

            if (orderLines.Any(x =>
                x.ProductGroupId != "1"))
            {
                throw new Exception(
                    "Paket kodu oluşturma şu anda yalnızca " +
                    "Çamaşır Kurutmalık (CK) için desteklenmektedir.");
            }


            // ---------------------------------------------------------
            // 3. CK KONFİGÜRASYON
            // ---------------------------------------------------------

            foreach (var line in orderLines)
            {
                if (line.CKConfiguration == null)
                {
                    throw new Exception(
                        $"{line.ProductName} için " +
                        "CK konfigürasyonu bulunamadı.");
                }
            }


            // ---------------------------------------------------------
            // 4. İLK KALEM
            // ---------------------------------------------------------

            var firstLine =
                orderLines.First();

            var firstConfig =
                firstLine.CKConfiguration!;


            // ---------------------------------------------------------
            // 5. KATALOG KODU
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                firstLine.CatalogCode))
            {
                throw new Exception(
                    $"{firstLine.ProductName} için " +
                    "katalog kodu bulunamadı.");
            }


            // ---------------------------------------------------------
            // 6. REFERANS
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                package.Referans))
            {
                throw new Exception(
                    "Paket referansı bulunamadı.");
            }

            var referans =
                package.Referans.Trim();


            // ---------------------------------------------------------
            // 7. AYNI ÜRÜN KONTROLÜ
            // ---------------------------------------------------------

            var distinctProducts =
                orderLines
                    .Select(x =>
                        x.CKConfiguration!.ProductId)
                    .Distinct()
                    .ToList();

            if (distinctProducts.Count > 1)
            {
                throw new Exception(
                    "CK karışık kolide farklı ürün kullanılamaz. " +
                    "Karışık koli aynı ürünün farklı " +
                    "konfigürasyonlarından oluşmalıdır.");
            }


            // ---------------------------------------------------------
            // 8. TEK / KARIŞIK KONFİGÜRASYON
            // ---------------------------------------------------------

            var distinctConfigurations =
                orderLines
                    .Select(x => new
                    {
                        x.CKConfiguration!.FootRal,
                        x.CKConfiguration.BodyWingRal,
                        x.CKConfiguration.PlasticColor1No,
                        x.CKConfiguration.PlasticColor2No
                    })
                    .Distinct()
                    .ToList();

            var isMixedPackage =
                distinctConfigurations.Count > 1;


            // ---------------------------------------------------------
            // 9. KOLİ SON 4 HANE
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                package.KoliKod))
            {
                throw new Exception(
                    "Paket için koli kodu bulunamadı.");
            }

            var koliSuffix =
                package.KoliKod.Length >= 4
                    ? package.KoliKod[^4..]
                    : package.KoliKod;


            // ---------------------------------------------------------
            // 10. PAKET RENGİ
            // ---------------------------------------------------------

            PaketRenk? paketRenk;

            if (!isMixedPackage)
            {
                paketRenk =
                    await _context.PaketRenkler
                        .FirstOrDefaultAsync(x =>
                            x.Ral1 ==
                                firstConfig.FootRal &&
                            x.Ral2 ==
                                firstConfig.BodyWingRal);

                if (paketRenk == null)
                {
                    throw new Exception(
                        "CK paket rengi bulunamadı. " +
                        $"RAL1={firstConfig.FootRal}, " +
                        $"RAL2={firstConfig.BodyWingRal}.");
                }
            }
            else
            {
                var secondLine =
                    orderLines[1];

                var secondConfig =
                    secondLine.CKConfiguration!;

                paketRenk =
                    await _context.PaketRenkler
                        .FirstOrDefaultAsync(x =>
                            x.Ral1 ==
                                firstConfig.FootRal &&
                            x.Ral2 ==
                                secondConfig.FootRal);

                if (paketRenk == null)
                {
                    throw new Exception(
                        "CK karışık paket rengi bulunamadı. " +
                        $"RAL1={firstConfig.FootRal}, " +
                        $"RAL2={secondConfig.FootRal}.");
                }
            }


            // ---------------------------------------------------------
            // 11. PAKET RENK NO
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                paketRenk.No))
            {
                throw new Exception(
                    "Paket renk tanımında NO bulunamadı.");
            }

            var paketRenkNo =
                paketRenk.No;


            // ---------------------------------------------------------
            // 12. PLASTİK KODLARI
            // ---------------------------------------------------------

            string plasticCode1;
            string plasticCode2;

            if (!isMixedPackage)
            {
                if (string.IsNullOrWhiteSpace(
                    firstConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{firstLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                plasticCode1 =
                    firstConfig.PlasticColor2No;

                plasticCode2 =
                    firstConfig.PlasticColor2No;
            }
            else
            {
                var secondLine =
                    orderLines[1];

                var secondConfig =
                    secondLine.CKConfiguration!;

                if (string.IsNullOrWhiteSpace(
                    firstConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{firstLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                if (string.IsNullOrWhiteSpace(
                    secondConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{secondLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                plasticCode1 =
                    firstConfig.PlasticColor2No;

                plasticCode2 =
                    secondConfig.PlasticColor2No;
            }


            // ---------------------------------------------------------
            // 13. PLASTİK RENK İSİMLERİ
            //
            // NO  -> Plastik kodu
            // RENK -> Paket adında kullanılacak isim
            // ---------------------------------------------------------

            var plasticColorNos =
                new[]
                {
                    plasticCode1,
                    plasticCode2
                }
                .Distinct()
                .ToList();

            var plasticColors =
                await _context.PlasticColors
                    .AsNoTracking()
                    .Where(x =>
                        plasticColorNos.Contains(x.No))
                    .ToListAsync();

            string GetPlasticColorName(
                string no)
            {
                var color =
                    plasticColors
                        .FirstOrDefault(x =>
                            x.No == no);

                if (color == null)
                {
                    throw new Exception(
                        "Plastik renk tanımı bulunamadı. " +
                        $"Plastik No={no}");
                }

                if (string.IsNullOrWhiteSpace(
                    color.Renk))
                {
                    throw new Exception(
                        "Plastik renk tanımında RENK bulunamadı. " +
                        $"Plastik No={no}");
                }

                return color.Renk.Trim();
            }

            var plasticColorName1 =
                GetPlasticColorName(
                    plasticCode1);

            var plasticColorName2 =
                GetPlasticColorName(
                    plasticCode2);


            // ---------------------------------------------------------
            // 14. PAKET KODU PREFIX
            //
            // Plastik burada KOD olarak kullanılır.
            //
            // Örnek:
            //
            // 11010.9005.0303.0097.
            // ---------------------------------------------------------

            var packageCodePrefix =
                $"{firstLine.CatalogCode}." +
                $"{paketRenkNo}." +
                $"{plasticCode1}{plasticCode2}." +
                $"{koliSuffix}.";


            // ---------------------------------------------------------
            // 15. MEVCUT PAKET KODLARI
            // ---------------------------------------------------------

            var existingCodesQuery =
                _context.SalesOrderPackages
                    .Where(x =>
                        x.NetsisPaketKodu != null &&
                        x.NetsisPaketKodu.StartsWith(
                            packageCodePrefix));

            if (excludePackageId.HasValue)
            {
                existingCodesQuery =
                    existingCodesQuery.Where(x =>
                        x.Id !=
                        excludePackageId.Value);
            }

            var existingCodes =
                await existingCodesQuery
                    .Select(x =>
                        x.NetsisPaketKodu!)
                    .ToListAsync();


            // ---------------------------------------------------------
            // 16. SON COUNTER
            // ---------------------------------------------------------

            var lastCounter = 0;

            foreach (var existingCode in existingCodes)
            {
                var lastPart =
                    existingCode
                        .Split('.')
                        .LastOrDefault();

                if (int.TryParse(
                    lastPart,
                    out var counter))
                {
                    if (counter > lastCounter)
                    {
                        lastCounter = counter;
                    }
                }
            }

            var newCounter =
                lastCounter + 1;

            var counterText =
                newCounter.ToString("D4");


            // ---------------------------------------------------------
            // 17. NETSİS PAKET KODU
            // ---------------------------------------------------------

            var netsisPaketKodu =
                $"{packageCodePrefix}" +
                $"{counterText}";


            // ---------------------------------------------------------
            // 18. PAKET ADI
            // ---------------------------------------------------------

            string packageName;

            if (!isMixedPackage)
            {
                // -----------------------------------------------------
                // TEK KONFİGÜRASYON
                //
                // Örnek:
                //
                // CK PKT CAPELLO 18 RAL 9005 SIYAH
                // -PLASTIK SIYAH 1 LI A01
                // -----------------------------------------------------

                packageName =
                    $"CK PKT " +
                    $"{firstLine.ProductName} " +
                    $"{paketRenk.Isim}" +
                    $"-PLASTIK " +
                    $"{plasticColorName1} " +
                    $"{package.KoliIciMiktar} LI " +
                    $"{referans}";
            }
            else
            {
                // -----------------------------------------------------
                // KARIŞIK KONFİGÜRASYON
                // -----------------------------------------------------

                var plasticText =
                    plasticCode1 == plasticCode2
                        ? plasticColorName1
                        : $"{plasticColorName1}/{plasticColorName2}";

                packageName =
                    $"CK PKT " +
                    $"{firstLine.ProductName} " +
                    $"{paketRenk.Isim}" +
                    $"-PLSTK " +
                    $"{plasticText} " +
                    $"{package.KoliIciMiktar} LI " +
                    $"{referans}";
            }


            // ---------------------------------------------------------
            // 19. BÜYÜK HARF
            // ---------------------------------------------------------

            packageName =
                packageName.ToUpper();


            // ---------------------------------------------------------
            // 20. SONUÇ
            // ---------------------------------------------------------

            return new PackageInfo
            {
                Code =
                    netsisPaketKodu,

                Name =
                    packageName
            };
        }
    }
}
