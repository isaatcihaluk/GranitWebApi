using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderPackageService : ISalesOrderPackageService
    {
        private readonly AppDbContext _context;
        private readonly ErpDbContext _erpContext;
        private readonly UserContext _userContext;

        private class PackageInfo
        {
            public string Code { get; set; } = null!;
            public string Name { get; set; } = null!;
        }

        private class RecipeComponent
        {
            public string StockCode { get; set; } = null!;
            public decimal Quantity { get; set; }
        }

        //private static void AddParameter(DbCommand command, string name, object? value)
        //{
        //    var parameter = command.CreateParameter();
        //    parameter.ParameterName = name;
        //    parameter.Value = value ?? DBNull.Value;
        //    command.Parameters.Add(parameter);
        //}

        public SalesOrderPackageService(AppDbContext context, ErpDbContext erpContext, UserContext userContext)
        {
            _context = context;
            _erpContext = erpContext;
            _userContext = userContext;
        }

        // PAKETLEME VERİLERİ
        public async Task<SalesOrder?> GetPackagingDataAsync(long salesOrderId)
        {
            var order = await _context.SalesOrders.AsNoTracking().Include(x => x.Lines)
                        .ThenInclude(x => x.CKConfiguration).Include(x => x.Lines)
                        .ThenInclude(x => x.PackageLines).Include(x => x.Packages)
                        .ThenInclude(x => x.Lines).FirstOrDefaultAsync(x => x.Id == salesOrderId);
            return order;
        }

        // PAKET OLUŞTUR
        public async Task<SalesOrderPackage> CreatePackageAsync(SalesOrderPackageCreateRequest request)
        {
            var order = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == request.SalesOrderId);

            if (order == null) { throw new Exception("Sipariş bulunamadı."); }
            if (order.Status != "PAKETLEME") { throw new Exception("Bu sipariş paketleme aşamasında değil."); }
            if (string.IsNullOrWhiteSpace(request.Referans)) { throw new Exception("Paket referansı girilmelidir."); }

            var referans = request.Referans.Trim();
            if (request.Lines == null || request.Lines.Count == 0) { throw new Exception("Pakete en az bir ürün eklenmelidir."); }

            // AYNI SİPARİŞ KALEMİ BİRDEN FAZLA EKLENEMEZ
            var duplicateLine = request.Lines.GroupBy(x => x.SalesOrderLineId).FirstOrDefault(x => x.Count() > 1);
            if (duplicateLine != null)
            {
                throw new Exception($"Sipariş kalemi {duplicateLine.Key} pakete birden fazla eklenemez.");
            }

            // KOLİ KONTROLÜ
            var koli = await _context.ProductBoxes.FirstOrDefaultAsync(x => x.Id == request.KoliId);

            if (koli == null) { throw new Exception("Seçilen koli bulunamadı."); }
            if (!koli.KoliIciMiktar.HasValue || koli.KoliIciMiktar.Value <= 0)
            {
                throw new Exception("Seçilen kolinin iç miktarı geçersiz.");
            }
            if (string.IsNullOrWhiteSpace(koli.KoliKod))
            {
                throw new Exception("Seçilen kolinin koli kodu bulunamadı.");
            }

            var koliIciMiktar = koli.KoliIciMiktar.Value;

            // SİPARİŞ KALEMLERİ
            var lineIds = request.Lines.Select(x => x.SalesOrderLineId).ToList();
            var orderLines = await _context.SalesOrderLines.Include(x => x.CKConfiguration).Include(x => x.UMConfiguration)
                .Where(x => x.SalesOrderId == request.SalesOrderId && lineIds.Contains(x.Id) && !x.DeletedFlag).ToListAsync();

            if (orderLines.Count != lineIds.Count)
            {
                throw new Exception(
                    "Pakete eklenen sipariş kalemlerinden biri " +
                    "bu siparişe ait değil.");
            }

            // AYNI PAKETTE FARKLI ÜRÜN GRUBU OLAMAZ
            var productGroupIds = orderLines.Select(x => x.ProductGroupId).Distinct().ToList();
            if (productGroupIds.Count != 1)
            {
                throw new Exception("Aynı pakette farklı ürün grupları kullanılamaz.");
            }

            // SHRINKLİ KOD KONTROLÜ
            foreach (var requestLine in request.Lines)
            {
                var orderLine = orderLines.First(x => x.Id == requestLine.SalesOrderLineId);

                if (string.IsNullOrWhiteSpace(orderLine.ShrinkliKod))
                {
                    throw new Exception(
                        $"{orderLine.ProductName} için " +
                        "shrinkli ürün kodu oluşturulmamış.");
                }
            }

            // MİKTAR KONTROLÜ
            foreach (var requestLine in request.Lines)
            {
                if (requestLine.Quantity <= 0)
                {
                    throw new Exception(
                        $"Sipariş kalemi {requestLine.SalesOrderLineId} için " +
                        "miktar 0'dan büyük olmalıdır.");
                }
            }

            var totalPackageQuantity = request.Lines.Sum(x => x.Quantity);
            if (totalPackageQuantity != koliIciMiktar)
            {
                throw new Exception(
                    $"Pakete eklenen toplam miktar {totalPackageQuantity} olmalıdır. " +
                    $"Seçilen kolinin kapasitesi {koliIciMiktar}.");
            }

            // PAKET NUMARASI
            var lastPackageNumber = await _context.SalesOrderPackages.Where(x => x.SalesOrderId == request.SalesOrderId)
                .Select(x => (int?)x.PackageNumber).MaxAsync() ?? 0;

            var newPackageNumber = lastPackageNumber + 1;
            var now = DateTime.Now;

            // PAKET OLUŞTUR
            var package = new SalesOrderPackage
            {
                SalesOrderId = request.SalesOrderId,
                PackageNumber = newPackageNumber,
                KoliId = koli.Id,
                KoliKod = koli.KoliKod,
                KoliIciMiktar = koliIciMiktar,
                Referans = referans,
                Status = "TASLAK",
                CreatedAt = now,
                CreatedBy = _userContext.UserId
            };

            // PAKET SATIRLARI
            foreach (var requestLine in request.Lines)
            {
                package.Lines.Add(
                    new SalesOrderPackageLine
                    {
                        SalesOrderLineId = requestLine.SalesOrderLineId,
                        Quantity = requestLine.Quantity,
                        CreatedAt = now,
                        CreatedBy = _userContext.UserId
                    });
            }

            var packageInfo = await GeneratePackageInfoAsync(package, orderLines);
            package.NetsisPaketKodu = packageInfo.Code;
            package.NetsisPaketAdi = packageInfo.Name;
            _context.SalesOrderPackages.Add(package);
            await _context.SaveChangesAsync();
            return package;
        }

        // PAKET GÜNCELLE
        public async Task<SalesOrderPackage> UpdatePackageAsync(long packageId, SalesOrderPackageCreateRequest request)
        {
            var order = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == request.SalesOrderId);
            if (order == null)
            {
                throw new Exception("Sipariş bulunamadı.");
            }
            if (order.Status != "PAKETLEME")
            {
                throw new Exception("Bu sipariş paketleme aşamasında değil.");
            }
            if (string.IsNullOrWhiteSpace(request.Referans))
            {
                throw new Exception("Paket referansı girilmelidir.");
            }

            var referans = request.Referans.Trim();

            var package = await _context.SalesOrderPackages.Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == packageId && x.SalesOrderId == request.SalesOrderId);
            if (package == null)
            {
                throw new Exception("Paket bulunamadı.");
            }
            if (package.Status != "TASLAK")
            {
                throw new Exception("Sadece TASLAK durumundaki paketler " + "güncellenebilir.");
            }
            if (request.Lines == null || request.Lines.Count == 0)
            {
                throw new Exception("Pakete en az bir ürün eklenmelidir.");
            }

            // AYNI SİPARİŞ KALEMİ BİRDEN FAZLA EKLENEMEZ
            var duplicateLine = request.Lines.GroupBy(x => x.SalesOrderLineId).FirstOrDefault(x => x.Count() > 1);
            if (duplicateLine != null)
            {
                throw new Exception($"Sipariş kalemi {duplicateLine.Key} " + $"pakete birden fazla eklenemez.");
            }

            // KOLİ KONTROLÜ
            var koli = await _context.ProductBoxes.FirstOrDefaultAsync(x => x.Id == request.KoliId);
            if (koli == null)
            {
                throw new Exception("Seçilen koli bulunamadı.");
            }
            if (!koli.KoliIciMiktar.HasValue || koli.KoliIciMiktar.Value <= 0)
            {
                throw new Exception("Seçilen kolinin iç miktarı geçersiz.");
            }
            if (string.IsNullOrWhiteSpace(koli.KoliKod))
            {
                throw new Exception("Seçilen kolinin koli kodu bulunamadı.");
            }

            var koliIciMiktar = koli.KoliIciMiktar.Value;

            // SİPARİŞ KALEMLERİ
            var lineIds = request.Lines.Select(x => x.SalesOrderLineId).ToList();
            var orderLines =
                await _context.SalesOrderLines.Include(x => x.CKConfiguration)
                    .Where(x => x.SalesOrderId == request.SalesOrderId &&
                        lineIds.Contains(x.Id) && !x.DeletedFlag).ToListAsync();
            if (orderLines.Count != lineIds.Count)
            {
                throw new Exception(
                    "Pakete eklenen sipariş kalemlerinden biri " +
                    "bu siparişe ait değil.");
            }

            // SHRINKLİ KOD KONTROLÜ
            foreach (var requestLine in request.Lines)
            {
                var orderLine = orderLines.First(x => x.Id == requestLine.SalesOrderLineId);
                if (string.IsNullOrWhiteSpace(orderLine.ShrinkliKod))
                {
                    throw new Exception(
                        $"{orderLine.ProductName} için " +
                        "shrinkli ürün kodu oluşturulmamış.");
                }
            }

            // MİKTAR KONTROLÜ
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

            var totalPackageQuantity = request.Lines.Sum(x => x.Quantity);
            if (totalPackageQuantity != koliIciMiktar)
            {
                throw new Exception(
                    $"Pakete eklenen toplam miktar " +
                    $"{totalPackageQuantity} olmalıdır. " +
                    $"Seçilen kolinin kapasitesi " +
                    $"{koliIciMiktar}.");
            }

            // TRANSACTION
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                package.KoliId = koli.Id;
                package.KoliKod = koli.KoliKod;
                package.KoliIciMiktar = koliIciMiktar;
                package.Referans = referans;
                package.UpdatedAt = now;
                package.UpdatedBy = _userContext.UserId;

                // ESKİ SATIRLARI KALDIR
                var oldPackageLines = package.Lines.ToList();
                _context.SalesOrderPackageLines.RemoveRange(oldPackageLines);
                package.Lines.Clear();

                // YENİ SATIRLARI EKLE
                foreach (var requestLine in request.Lines)
                {
                    package.Lines.Add(
                        new SalesOrderPackageLine
                        {
                            SalesOrderPackageId = package.Id,
                            SalesOrderLineId = requestLine.SalesOrderLineId,
                            Quantity = requestLine.Quantity,
                            CreatedAt = now,
                            CreatedBy = _userContext.UserId
                        });
                }

                // PAKET KODU / ADI YENİDEN OLUŞTUR
                var packageInfo = await GenerateCkPackageInfoAsync(package, orderLines, package.Id);
                package.NetsisPaketKodu = packageInfo.Code;
                package.NetsisPaketAdi = packageInfo.Name;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return package;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PAKET İPTAL
        public async Task CancelPackageAsync(long packageId, long salesOrderId)
        {
            var package = await _context.SalesOrderPackages.FirstOrDefaultAsync(x => x.Id == packageId && x.SalesOrderId == salesOrderId);
            if (package == null)
            {
                throw new Exception("Paket bulunamadı.");
            }
            if (package.Status != "TASLAK")
            {
                throw new Exception("Sadece TASLAK durumundaki paketler " + "iptal edilebilir.");
            }
            package.Status = "IPTAL";
            package.UpdatedAt = DateTime.Now;
            package.UpdatedBy = _userContext.UserId;
            await _context.SaveChangesAsync();
        }

        private async Task<PackageInfo> GeneratePackageInfoAsync(SalesOrderPackage package,
            List<SalesOrderLine> orderLines, long? excludePackageId = null)
        {
            if (orderLines == null || orderLines.Count == 0) { throw new Exception("Paket için sipariş kalemi bulunamadı."); }
            var productGroupIds = orderLines.Select(x => x.ProductGroupId).Distinct().ToList();

            if (productGroupIds.Count != 1)
            {
                throw new Exception("Aynı pakette farklı ürün grupları kullanılamaz.");
            }

            return productGroupIds[0] switch
            {
                "1" => await GenerateCkPackageInfoAsync(package, orderLines, excludePackageId),
                "2" => await GenerateUmPackageInfoAsync(package, orderLines, excludePackageId),
                "5" => await GenerateYkPackageInfoAsync(package, orderLines),
                _ => throw new Exception(
                    $"ProductGroupId={productGroupIds[0]} için " +
                    "paket kodu oluşturma desteklenmiyor.")
            };
        }

        // CK PAKET KODU / ADI OLUŞTUR
        private async Task<PackageInfo> GenerateCkPackageInfoAsync(SalesOrderPackage package,
            List<SalesOrderLine> orderLines, long? excludePackageId = null)
        {
            if (orderLines == null || orderLines.Count == 0)
            {
                throw new Exception("Paket için sipariş kalemi bulunamadı.");
            }
            if (orderLines.Any(x => x.ProductGroupId != "1"))
            {
                throw new Exception(
                    "Paket kodu oluşturma şu anda yalnızca " +
                    "Çamaşır Kurutmalık (CK) için desteklenmektedir.");
            }
            foreach (var line in orderLines)
            {
                if (line.CKConfiguration == null)
                {
                    throw new Exception($"{line.ProductName} için " + "CK konfigürasyonu bulunamadı.");
                }
            }

            var firstLine = orderLines.First();
            var firstConfig = firstLine.CKConfiguration!;
            if (string.IsNullOrWhiteSpace(firstLine.CatalogCode))
            {
                throw new Exception(
                    $"{firstLine.ProductName} için " +
                    "katalog kodu bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(package.Referans))
            {
                throw new Exception("Paket referansı bulunamadı.");
            }

            var referans = package.Referans.Trim();
            var distinctProducts = orderLines.Select(x => x.CKConfiguration!.ProductId).Distinct().ToList();
            if (distinctProducts.Count > 1)
            {
                throw new Exception(
                    "CK karışık kolide farklı ürün kullanılamaz. " +
                    "Karışık koli aynı ürünün farklı " +
                    "konfigürasyonlarından oluşmalıdır.");
            }

            // KONFİGÜRASYON KONTROLÜ
            var distinctConfigurations =
                orderLines
                    .Select(x => new
                    {
                        x.CKConfiguration!.FootRal,
                        x.CKConfiguration.BodyWingRal,
                        x.CKConfiguration.PlasticColor1No,
                        x.CKConfiguration.PlasticColor2No
                    }).Distinct().ToList();

            var isMixedPackage = distinctConfigurations.Count > 1;
            if (string.IsNullOrWhiteSpace(package.KoliKod))
            {
                throw new Exception("Paket için koli kodu bulunamadı.");
            }

            var koliSuffix = package.KoliKod.Length >= 4 ? package.KoliKod[^4..] : package.KoliKod;
            PaketRenk? paketRenk;

            if (!isMixedPackage)
            {
                paketRenk =
                    await _context.PaketRenkler
                        .FirstOrDefaultAsync(x =>
                            x.Ral1 == firstConfig.FootRal &&
                            x.Ral2 == firstConfig.BodyWingRal);

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
                var secondLine = orderLines[1];
                var secondConfig = secondLine.CKConfiguration!;

                paketRenk = await _context.PaketRenkler
                        .FirstOrDefaultAsync(x =>
                            x.Ral1 == firstConfig.FootRal &&
                            x.Ral2 == secondConfig.FootRal);

                if (paketRenk == null)
                {
                    throw new Exception(
                        "CK karışık paket rengi bulunamadı. " +
                        $"RAL1={firstConfig.FootRal}, " +
                        $"RAL2={secondConfig.FootRal}.");
                }
            }

            if (string.IsNullOrWhiteSpace(paketRenk.No))
            {
                throw new Exception("Paket renk tanımında NO bulunamadı.");
            }

            var paketRenkNo = paketRenk.No;
            string plasticCode1;
            string plasticCode2;

            if (!isMixedPackage)
            {
                if (string.IsNullOrWhiteSpace(firstConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{firstLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                plasticCode1 = firstConfig.PlasticColor2No;
                plasticCode2 = firstConfig.PlasticColor2No;
            }
            else
            {
                var secondLine = orderLines[1];
                var secondConfig = secondLine.CKConfiguration!;
                if (string.IsNullOrWhiteSpace(firstConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{firstLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                if (string.IsNullOrWhiteSpace(secondConfig.PlasticColor2No))
                {
                    throw new Exception(
                        $"{secondLine.ProductName} için " +
                        "plastik renk kodu bulunamadı.");
                }

                plasticCode1 = firstConfig.PlasticColor2No;
                plasticCode2 = secondConfig.PlasticColor2No;
            }

            var plasticColorNos = new[] { plasticCode1, plasticCode2 }.Distinct().ToList();

            var plasticColors =
                await _context.PlasticColors.AsNoTracking().Where(x => plasticColorNos.Contains(x.No)).ToListAsync();

            string GetPlasticColorName(string no)
            {
                var color = plasticColors.FirstOrDefault(x => x.No == no);
                if (color == null)
                {
                    throw new Exception(
                        "Plastik renk tanımı bulunamadı. " +
                        $"Plastik No={no}");
                }
                if (string.IsNullOrWhiteSpace(color.Renk))
                {
                    throw new Exception(
                        "Plastik renk tanımında RENK bulunamadı. " +
                        $"Plastik No={no}");
                }
                return color.Renk.Trim();
            }

            var plasticColorName1 = GetPlasticColorName(plasticCode1);
            var plasticColorName2 = GetPlasticColorName(plasticCode2);

            // PAKET KODU PREFIX
            var packageCodePrefix =
                $"{firstLine.CatalogCode}." +
                $"{paketRenkNo}." +
                $"{plasticCode1}{plasticCode2}." +
                $"{koliSuffix}.";

            var expectedRecipe = await BuildExpectedRecipeAsync(package, orderLines);
            var matchingPackageCode =
                await FindMatchingNetsisPackageCodeAsync(packageCodePrefix, expectedRecipe, excludePackageId);

            string netsisPaketKodu;
            if (!string.IsNullOrWhiteSpace(matchingPackageCode))
            {
                netsisPaketKodu = matchingPackageCode;
            }
            else
            {
                var netsisPackageCodes =
                    await _erpContext.TBLSTOKURM
                        .AsNoTracking()
                        .Where(x => x.MAMUL_KODU != null && x.MAMUL_KODU.StartsWith(packageCodePrefix))
                        .Select(x => x.MAMUL_KODU!).Distinct().ToListAsync();

                var localPackageCodesQuery = _context.SalesOrderPackages.AsNoTracking()
                    .Where(x => x.NetsisPaketKodu != null && x.NetsisPaketKodu.StartsWith(packageCodePrefix));

                if (excludePackageId.HasValue)
                {
                    localPackageCodesQuery = localPackageCodesQuery.Where(x => x.Id != excludePackageId.Value);
                }

                var localPackageCodes = await localPackageCodesQuery.Select(x => x.NetsisPaketKodu!).ToListAsync();
                var allPackageCodes = netsisPackageCodes.Concat(localPackageCodes)
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var lastCounter = 0;

                foreach (var code in allPackageCodes)
                {
                    var lastPart = code.Split('.').LastOrDefault();
                    if (int.TryParse(lastPart, out var counter))
                    {
                        if (counter > lastCounter)
                        {
                            lastCounter = counter;
                        }
                    }
                }
                var newCounter = lastCounter + 1;
                var counterText = newCounter.ToString("D4");
                netsisPaketKodu = $"{packageCodePrefix}" + $"{counterText}";
            }

            // PAKET ADI
            string packageName;
            if (!isMixedPackage)
            {
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
                var plasticText = plasticCode1 == plasticCode2 ? plasticColorName1 : $"{plasticColorName1}/{plasticColorName2}";
                packageName =
                    $"CK PKT " +
                    $"{firstLine.ProductName} " +
                    $"{paketRenk.Isim}" +
                    $"-PLSTK " +
                    $"{plasticText} " +
                    $"{package.KoliIciMiktar} LI " +
                    $"{referans}";
            }

            packageName = packageName.ToUpper();
            return new PackageInfo
            {
                Code = netsisPaketKodu,
                Name = packageName
            };
        }

        // UM PAKET KODU / ADI OLUŞTUR
        private async Task<PackageInfo> GenerateUmPackageInfoAsync(SalesOrderPackage package,
            List<SalesOrderLine> orderLines, long? excludePackageId = null)
        {
            if (orderLines == null || orderLines.Count == 0)
            {
                throw new Exception("Paket için sipariş kalemi bulunamadı.");
            }

            if (orderLines.Any(x => x.ProductGroupId != "2"))
            {
                throw new Exception(
                    "Paket kodu oluşturma şu anda yalnızca " +
                    "Ütü Masası (UM) için desteklenmektedir.");
            }

            // İlk 2 ürün yalnızca paket kodu ve paket adı için kullanılır.
            // Reçete tüm orderLines üzerinden oluşturulur.
            var packageLines = orderLines.Take(2).ToList();

            foreach (var line in packageLines)
            {
                if (line.UMConfiguration == null)
                {
                    throw new Exception($"{line.ProductName} için UM konfigürasyonu bulunamadı.");
                }
            }

            var firstLine = packageLines.First();
            var firstConfig = firstLine.UMConfiguration!;

            if (string.IsNullOrWhiteSpace(firstLine.CatalogCode))
            {
                throw new Exception($"{firstLine.ProductName} için katalog kodu bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(package.Referans))
            {
                throw new Exception("Paket referansı bulunamadı.");
            }

            var referans = package.Referans.Trim();

            // İLK 2 ÜRÜN
            var secondLine = packageLines.Count > 1 ? packageLines[1] : null;
            var secondConfig = secondLine?.UMConfiguration;

            // FABRIC
            if (string.IsNullOrWhiteSpace(firstConfig.FabricName))
            {
                throw new Exception($"{firstLine.ProductName} için kumaş adı bulunamadı.");
            }

            var fabricValue1 = firstConfig.FabricName.Trim();
            if (fabricValue1.Length < 4)
            {
                throw new Exception(
                    $"{firstLine.ProductName} için FabricName değeri " +
                    "en az 4 karakter olmalıdır.");
            }

            fabricValue1 = fabricValue1[^4..];
            string fabricValue2;
            if (secondConfig == null)
            {
                fabricValue2 = fabricValue1;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(secondConfig.FabricName))
                {
                    throw new Exception($"{secondLine!.ProductName} için kumaş adı bulunamadı.");
                }

                fabricValue2 = secondConfig.FabricName.Trim();
                if (fabricValue2.Length < 4)
                {
                    throw new Exception(
                        $"{secondLine.ProductName} için FabricName değeri " +
                        "en az 4 karakter olmalıdır.");
                }
                fabricValue2 = fabricValue2[^4..];
            }

            // RAL
            if (string.IsNullOrWhiteSpace(firstConfig.BodyIroningRal))
            {
                throw new Exception($"{firstLine.ProductName} için ürün RAL bilgisi bulunamadı.");
            }
            var paketRal1 = firstConfig.BodyIroningRal.Trim();
            var paketRal2 = secondConfig != null && !string.IsNullOrWhiteSpace(secondConfig.BodyIroningRal)
                    ? secondConfig.BodyIroningRal.Trim() : paketRal1;

            if (string.IsNullOrWhiteSpace(firstConfig.PlasticCombinationNo))
            {
                throw new Exception(
                    $"{firstLine.ProductName} için " +
                    "plastik kombinasyon numarası bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(firstConfig.PlasticCombinationDescription))
            {
                throw new Exception(
                    $"{firstLine.ProductName} için " +
                    "plastik kombinasyon açıklaması bulunamadı.");
            }

            var plasticCombinationNo1 = firstConfig.PlasticCombinationNo.Trim();
            string plasticCombinationNo2;
            if (secondConfig == null)
            {
                plasticCombinationNo2 = plasticCombinationNo1;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(secondConfig.PlasticCombinationNo))
                {
                    throw new Exception(
                        $"{secondLine!.ProductName} için " +
                        "plastik kombinasyon numarası bulunamadı.");
                }
                plasticCombinationNo2 = secondConfig.PlasticCombinationNo.Trim();
            }

            var plasticDescription1 = firstConfig.PlasticCombinationDescription.Trim();
            string plasticDescription2;

            if (secondConfig == null)
            {
                plasticDescription2 = plasticDescription1;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(secondConfig.PlasticCombinationDescription))
                {
                    throw new Exception(
                        $"{secondLine!.ProductName} için " +
                        "plastik kombinasyon açıklaması bulunamadı.");
                }
                plasticDescription2 = secondConfig.PlasticCombinationDescription.Trim();
            }
            string plasticCombinationDescription;

            if (string.Equals(plasticDescription1, plasticDescription2, StringComparison.OrdinalIgnoreCase))
            {
                plasticCombinationDescription = plasticDescription1;
            }
            else
            {
                plasticCombinationDescription = $"{plasticDescription1}/{plasticDescription2}";
            }

            PaketRenk? paketRenk = await _context.PaketRenkler.AsNoTracking()
                    .FirstOrDefaultAsync(x => (x.Ral1 == paketRal1 && x.Ral2 == paketRal2) || (x.Ral1 == paketRal2 && x.Ral2 == paketRal1));

            if (paketRenk == null)
            {
                throw new Exception(
                    "UM paket rengi bulunamadı. " +
                    $"RAL1={paketRal1}, RAL2={paketRal2}.");
            }
            if (string.IsNullOrWhiteSpace(paketRenk.No))
            {
                throw new Exception("Paket renk tanımında NO bulunamadı.");
            }
            var paketRenkNo = paketRenk.No.Trim();
            if (string.IsNullOrWhiteSpace(package.KoliKod))
            {
                throw new Exception("Paket için koli kodu bulunamadı.");
            }

            var koliSuffix = package.KoliKod.Length >= 4 ? package.KoliKod[^4..] : package.KoliKod;

            var packageCodePrefix =
                $"{firstLine.CatalogCode}." +
                $"{paketRenkNo}." +
                $"{plasticCombinationNo1}{plasticCombinationNo2}." +
                $"{fabricValue1}." +
                $"{fabricValue2}." +
                $"{koliSuffix}.";

            var expectedRecipe = await BuildExpectedRecipeAsync(package, orderLines);
            var matchingPackageCode = await FindMatchingNetsisPackageCodeAsync(packageCodePrefix, expectedRecipe, excludePackageId);
            string netsisPaketKodu;
            if (!string.IsNullOrWhiteSpace(matchingPackageCode))
            {
                netsisPaketKodu = matchingPackageCode;
            }
            else
            {
                var netsisPackageCodes = await _erpContext.TBLSTOKURM.AsNoTracking()
                        .Where(x => x.MAMUL_KODU != null && x.MAMUL_KODU.StartsWith(packageCodePrefix))
                        .Select(x => x.MAMUL_KODU!).Distinct().ToListAsync();

                var localPackageCodesQuery = _context.SalesOrderPackages.AsNoTracking()
                        .Where(x => x.NetsisPaketKodu != null && x.NetsisPaketKodu.StartsWith(packageCodePrefix));

                if (excludePackageId.HasValue)
                {
                    localPackageCodesQuery = localPackageCodesQuery.Where(x => x.Id != excludePackageId.Value);
                }

                var localPackageCodes = await localPackageCodesQuery.Select(x => x.NetsisPaketKodu!).ToListAsync();
                var allPackageCodes = netsisPackageCodes.Concat(localPackageCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var lastCounter = 0;
                foreach (var code in allPackageCodes)
                {
                    var lastPart = code.Split('.').LastOrDefault();

                    if (int.TryParse(lastPart, out var counter))
                    {
                        if (counter > lastCounter)
                        {
                            lastCounter = counter;
                        }
                    }
                }
                var newCounter = lastCounter + 1;
                var counterText = newCounter.ToString("D4");
                netsisPaketKodu = $"{packageCodePrefix}{counterText}";
            }

            var packageName =
                $"UM PKT " +
                $"{firstLine.ProductName} " +
                $"{paketRenk.Isim}" +
                $"-PLSTK " +
                $"{plasticCombinationDescription} " +
                $"{package.KoliIciMiktar} LI " +
                $"{referans}";

            packageName = packageName.ToUpper();
            return new PackageInfo
            {
                Code = netsisPaketKodu,
                Name = packageName
            };
        }

        // YK PAKET KODU / ADI OLUŞTUR
        private async Task<PackageInfo> GenerateYkPackageInfoAsync(SalesOrderPackage package,List<SalesOrderLine> orderLines)
        {
            if (orderLines == null || orderLines.Count != 1)
            {
                throw new Exception("YK paketi için yalnızca bir sipariş kalemi bulunmalıdır.");
            }

            var line = orderLines[0];
            if (line.ProductGroupId != "5")
            {
                throw new Exception("YK paket bilgisi yalnızca Yedek Kılıf ürün grubu için oluşturulabilir.");
            }
            if (string.IsNullOrWhiteSpace(line.CatalogCode))
            {
                throw new Exception($"Satır {line.LineNumber} için YK katalog kodu bulunamadı.");
            }
            if (string.IsNullOrWhiteSpace(line.YKFabricCode))
            {
                throw new Exception($"{line.ProductName} için YK kumaş kodu bulunamadı.");
            }

            var fabricParts = line.YKFabricCode.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (fabricParts.Length < 3)
            {
                throw new Exception(
                    $"{line.ProductName} için kumaş kodu formatı geçersiz: " +
                    $"{line.YKFabricCode}");
            }

            var fabricCode = fabricParts[2].Trim();
            if (string.IsNullOrWhiteSpace(fabricCode))
            {
                throw new Exception($"{line.ProductName} için kumaş kodu alınamadı.");
            }

            // Varsayılan broşür kodu
            var lastThreeChars = "000";

            // ImageTypeId = 1 -> Broşür görseli
            var brosurImage = await _context.SalesOrderLineImages
                .FirstOrDefaultAsync(x =>
                    x.SalesOrderLineId == line.Id &&
                    x.ImageTypeId == 1 &&
                    x.IsActive);

            SalesOrderLineTechnicalItem? technicalItem = null;

            // Broşür görseli varsa teknik stok kontrol edilir
            if (brosurImage != null)
            {
                technicalItem = await _context.SalesOrderLineTechnicalItems
                    .FirstOrDefaultAsync(x =>x.SalesOrderLineId == line.Id && x.ImageId == brosurImage.Id && x.IsActive);

                if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockCode))
                {
                    var stockCode = technicalItem.StockCode.Trim();
                    if (stockCode.Length >= 3)
                    {
                        lastThreeChars = stockCode.Substring(stockCode.Length - 3);
                    }
                }
            }

            // YK paket kodu:
            var packageCode =    $"{line.CatalogCode.Trim()}.{fabricCode}.0092.{lastThreeChars}";

            // Paket adı
            string brosurSuffix;
            if (lastThreeChars == "000")
            {
                brosurSuffix = "BROSURSUZ";
            }
            else if (lastThreeChars == "999")
            {
                brosurSuffix = "GRANIT BROSUR";
            }
            else
            {
                if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockName))
                {
                    var match = Regex.Match(technicalItem.StockName,@"\(([^()]*)\)");

                    if (match.Success)
                    {
                        brosurSuffix = match.Groups[1].Value.Trim();
                    }
                    else
                    {
                        throw new Exception(
                            "YK paket adı oluşturulamadı. " +
                            "Broşür stok adında parantez içerisinde referans/barkod bulunamadı.");
                    }
                }
                else
                {
                    throw new Exception(
                        "YK paket adı oluşturulamadı. " +
                        "Broşür stok adı bulunamadı.");
                }
            }

            // YEDEK KILIF KECELI TEK TARAFLI K1020 GRANIT BROSUR
            var packageName = $"{line.ProductName?.Trim()} K{fabricCode} {brosurSuffix}".Trim();
            return new PackageInfo
            {
                Code = packageCode,
                Name = packageName
            };
        }

        // PAKETLEMEYİ TAMAMLA
        public async Task CompletePackagingAsync(long salesOrderId)
        {
            var order = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (order == null)
            {
                throw new Exception("Sipariş bulunamadı.");
            }

            if (order.Status != "PAKETLEME")
            {
                throw new Exception("Bu sipariş paketleme aşamasında değil.");
            }

            var orderLines = await _context.SalesOrderLines
                .Where(x =>x.SalesOrderId == salesOrderId && !x.DeletedFlag).ToListAsync();

            if (orderLines.Count == 0)
            {
                throw new Exception("Siparişte aktif sipariş kalemi bulunamadı.");
            }

            var hasPhysicalPackagingRequired = orderLines.Any(x =>
                x.ProductGroupId == "1" ||
                x.ProductGroupId == "2");

            var hasActivePackage = await _context.SalesOrderPackages
                .AnyAsync(x =>
                    x.SalesOrderId == salesOrderId &&
                    x.Status != "IPTAL");

            // CK / UM varsa fiziksel paket oluşturulmuş olmalıdır.
            // Sadece YK siparişinde fiziksel paket zorunlu değildir.
            if (hasPhysicalPackagingRequired && !hasActivePackage)
            {
                throw new Exception(
                    "Paketleme tamamlanmadan önce " +
                    "en az bir paket oluşturulmalıdır.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // YK OTOMATİK PAKET OLUŞTURMA

                var ykLines = orderLines.Where(x => x.ProductGroupId == "5").ToList();
                if (ykLines.Count > 0)
                {
                    var lastPackageNumber = await _context.SalesOrderPackages
                        .Where(x => x.SalesOrderId == salesOrderId)
                        .Select(x => (int?)x.PackageNumber)
                        .MaxAsync() ?? 0;

                    var nextPackageNumber = lastPackageNumber + 1;
                    var now = DateTime.Now;

                    foreach (var ykLine in ykLines)
                    {
                        var package = new SalesOrderPackage
                        {
                            SalesOrderId = salesOrderId,
                            PackageNumber = nextPackageNumber,

                            // YK fiziksel koli kullanmaz.
                            // Sistemsel olarak özel koli değeri kullanılır.
                            KoliId = "9999999",
                            KoliKod= "3301.2232.340.0510.0240.0092",
                            KoliIciMiktar=15,

                            Status = "TASLAK",
                            CreatedAt = now,
                            CreatedBy = _userContext.UserId
                        };

                        var packageInfo = await GenerateYkPackageInfoAsync(package,new List<SalesOrderLine> { ykLine });

                        package.NetsisPaketKodu = packageInfo.Code;
                        package.NetsisPaketAdi = packageInfo.Name;

                        package.Lines.Add(
                            new SalesOrderPackageLine
                            {
                                SalesOrderLineId = ykLine.Id,
                                Quantity = ykLine.Quantity,
                                CreatedAt = now,
                                CreatedBy = _userContext.UserId
                            });

                        _context.SalesOrderPackages.Add(package);
                        nextPackageNumber++;
                    }
                    await _context.SaveChangesAsync();
                }

                // PAKETLEMEYİ TAMAMLA
                order.Status = "FIYATLANDIRMA";
                order.UpdatedAt = DateTime.Now;
                order.UpdatedBy = _userContext.UserId;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception(ex.InnerException?.Message ?? ex.Message,ex); 
            }
        }
        // BEKLENEN REÇETE OLUŞTUR
        private async Task<List<RecipeComponent>> BuildExpectedRecipeAsync(SalesOrderPackage package, List<SalesOrderLine> orderLines)
        {
            var result = new List<RecipeComponent>();
            foreach (var packageLine in package.Lines)
            {
                var orderLine = orderLines.FirstOrDefault(x => x.Id == packageLine.SalesOrderLineId);
                if (orderLine == null)
                {
                    throw new Exception(
                        "Paket içerisindeki sipariş kalemi " +
                        "bulunamadı. " +
                        $"LineId={packageLine.SalesOrderLineId}");
                }

                if (string.IsNullOrWhiteSpace(orderLine.ShrinkliKod))
                {
                    throw new Exception(
                        $"{orderLine.ProductName} için " +
                        "shrinkli ürün kodu bulunamadı.");
                }

                // 1. Shrinkli ürün

                result.Add(
                    new RecipeComponent
                    {
                        StockCode = orderLine.ShrinkliKod.Trim(),
                        Quantity = packageLine.Quantity
                    });

                var technicalItems = await _context.SalesOrderLineTechnicalItems
                        .Include(x => x.Image).Where(x => x.SalesOrderLineId == orderLine.Id &&
                        x.IsActive && x.Image != null && new[] { 5, 6, 7, 8 }
                        .Contains(x.Image.ImageTypeId) && !string.IsNullOrWhiteSpace(x.StockCode)).ToListAsync();

                foreach (var technicalItem in technicalItems)
                {
                    result.Add(
                        new RecipeComponent
                        {
                            StockCode = technicalItem.StockCode.Trim(),
                            Quantity = 1
                        });
                }
            }

            // 3. Koli

            if (string.IsNullOrWhiteSpace(package.KoliKod))
            {
                throw new Exception("Paket için koli kodu bulunamadı.");
            }

            result.Add(
                new RecipeComponent
                {
                    StockCode = package.KoliKod.Trim(),
                    Quantity = 1
                });

            // Aynı stok kodlarını birleştir

            return result.GroupBy(x => x.StockCode, StringComparer.OrdinalIgnoreCase)
                .Select(x =>
                    new RecipeComponent
                    {
                        StockCode = x.First().StockCode,
                        Quantity = x.Sum(y => y.Quantity)
                    }).ToList();
        }

        // NETSİS PAKET KODU EŞLEŞTİR
        private async Task<string?> FindMatchingNetsisPackageCodeAsync(string packageCodePrefix,
                List<RecipeComponent> expectedRecipe, long? excludePackageId = null)
        {
            var candidateCodes = await _erpContext.TBLSTOKURM.AsNoTracking()
                    .Where(x => x.MAMUL_KODU != null && x.MAMUL_KODU.StartsWith(packageCodePrefix))
                    .Select(x => x.MAMUL_KODU!).Distinct().ToListAsync();

            if (candidateCodes.Count == 0)
            {
                return null;
            }

            // SİSTEMDE KULLANILAN PAKET KODLARI
            var usedPackageCodesQuery = _context.SalesOrderPackages.Where(x => x.NetsisPaketKodu != null);
            if (excludePackageId.HasValue)
            {
                usedPackageCodesQuery = usedPackageCodesQuery.Where(x => x.Id != excludePackageId.Value);
            }

            var usedPackageCodes = await usedPackageCodesQuery.Select(x => x.NetsisPaketKodu!).ToListAsync();
            var usedPackageCodeSet = new HashSet<string>(usedPackageCodes, StringComparer.OrdinalIgnoreCase);

            // ADAYLARI KONTROL ET
            foreach (var candidateCode in candidateCodes)
            {
                if (usedPackageCodeSet.Contains(candidateCode))
                {
                    continue;
                }

                var recipe =
                    await _erpContext.TBLSTOKURM.AsNoTracking()
                        .Where(x => x.MAMUL_KODU == candidateCode && x.HAM_KODU != null)
                        .Select(x =>
                            new RecipeComponent
                            {
                                StockCode = x.HAM_KODU!,
                                Quantity = x.MIKTAR ?? 0
                            })
                        .ToListAsync();

                if (IsRecipeMatch(expectedRecipe, recipe))
                {
                    return candidateCode;
                }
            }
            return null;
        }

        // REÇETE KONTROL
        private bool IsRecipeMatch(List<RecipeComponent> expectedRecipe, List<RecipeComponent> netsisRecipe)
        {
            var expected = expectedRecipe
                .GroupBy(x => x.StockCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.OrdinalIgnoreCase);

            var actual = netsisRecipe
                .GroupBy(x => x.StockCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.OrdinalIgnoreCase);

            foreach (var item in expected)
            {
                if (!actual.TryGetValue(item.Key, out var actualQuantity))
                {
                    return false;
                }
                if (actualQuantity != item.Value)
                {
                    return false;
                }
            }
            return true;
        }

        // FİYATLANDIRMA VERİLERİ
        public async Task<SalesOrder?> GetPricingDataAsync(long salesOrderId)
        {
            var order =
                await _context.SalesOrders.AsNoTracking().Include(x => x.Lines)
                    .Include(x => x.Packages.Where(p => p.Status != "IPTAL"))
                    .ThenInclude(p => p.Lines).FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (order == null) { return null; }
            if (order.Status != "FIYATLANDIRMA" && order.Status != "NETSIS_AKTARIM_BEKLIYOR" && order.Status != "TAMAMLANDI")
            {
                throw new Exception("Sipariş fiyatlandırma veya son kontrol aşamasında değil.");
            }
            return order;
        }

        public async Task SavePricingAsync(SalesOrderPricingRequest request)
        {
            var order = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == request.SalesOrderId);
            if (order == null)
            {
                throw new Exception("Sipariş bulunamadı.");
            }
            if (request.Packages == null || request.Packages.Count == 0)
            {
                throw new Exception("Fiyatlandırılacak paket bulunamadı.");
            }
            // AKTİF PAKETLER
            var packages = await _context.SalesOrderPackages.Include(x => x.Lines)
                .Where(x => x.SalesOrderId == request.SalesOrderId && x.Status != "IPTAL").ToListAsync();

            if (packages.Count == 0)
            {
                throw new Exception("Siparişe ait aktif paket bulunamadı.");
            }

            // SİPARİŞ SATIRLARI
            var orderLines = await _context.SalesOrderLines
                .Where(x => x.SalesOrderId == request.SalesOrderId && !x.DeletedFlag).ToListAsync();

            // REQUEST PAKET ID KONTROLLERİ
            var requestPackageIds = request.Packages.Select(x => x.PackageId).ToList();
            if (requestPackageIds.Count != requestPackageIds.Distinct().Count())
            {
                throw new Exception("Aynı paket için birden fazla fiyat gönderilemez.");
            }

            // TÜM AKTİF PAKETLERİN FİYATI GİRİLMİŞ Mİ?
            var missingPackages = packages.Where(x => !requestPackageIds.Contains(x.Id)).ToList();
            if (missingPackages.Count > 0)
            {
                throw new Exception("Tüm aktif paketler için fiyat girilmelidir.");
            }

            // GEÇERSİZ PAKET KONTROLÜ
            var invalidPackages = request.Packages.Where(x => !packages.Any(p => p.Id == x.PackageId)).ToList();
            if (invalidPackages.Count > 0)
            {
                throw new Exception("Siparişe ait olmayan bir paket için fiyat gönderildi.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;

                foreach (var package in packages)
                {
                    var pricing = request.Packages.FirstOrDefault(x => x.PackageId == package.Id);
                    if (pricing == null)
                    {
                        throw new Exception($"Paket fiyatı bulunamadı. Paket No={package.PackageNumber}");
                    }
                    // BİRİM FİYAT
                    if (pricing.UnitPrice <= 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için birim fiyat 0'dan büyük olmalıdır.");
                    }
                    // KOLİ İÇİ MİKTAR
                    if (package.KoliIciMiktar <= 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için koli içi miktar geçersiz.");
                    }

                    // PAKETTEKİ SİPARİŞ SATIRLARI
                    var packageLineIds = package.Lines.Select(x => x.SalesOrderLineId).Distinct().ToList();
                    if (packageLineIds.Count == 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için sipariş satırı bulunamadı.");
                    }

                    var packageOrderLines = orderLines.Where(x => packageLineIds.Contains(x.Id)).ToList();
                    if (packageOrderLines.Count == 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için geçerli sipariş satırı bulunamadı.");
                    }

                    // PAKETTEKİ TOPLAM ÜRÜN MİKTARI
                    var packageQuantity = packageOrderLines.Sum(x => x.Quantity);
                    if (packageQuantity <= 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için paket miktarı hesaplanamadı.");
                    }

                    // KOLİ ADEDİ
                    var koliAdedi = packageQuantity / package.KoliIciMiktar;
                    if (koliAdedi <= 0)
                    {
                        throw new Exception($"Paket No={package.PackageNumber} için koli adedi hesaplanamadı.");
                    }

                    var unitKoliPrice = pricing.UnitPrice * package.KoliIciMiktar;
                    var totalPrice = packageQuantity * pricing.UnitPrice;

                    // PAKETİ GÜNCELLE
                    package.PackageQuantity = packageQuantity;
                    package.KoliAdedi = koliAdedi;
                    package.UnitPrice = pricing.UnitPrice;
                    package.UnitKoliPrice = unitKoliPrice;
                    package.TotalPrice = totalPrice;
                    package.UpdatedAt = now;
                    package.Definition = pricing.Definition?.Trim();
                    package.UpdatedBy = _userContext.UserId;
                }

                // SİPARİŞ DURUMU
                order.Status = "NETSIS_AKTARIM_BEKLIYOR";
                order.Definition = request.Definition?.Trim();
                order.UpdatedAt = now;
                order.UpdatedBy = _userContext.UserId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // NETSIS REVIZE MIKTAR FIYAT
        public async Task UpdateRevisionPricingAsync(SalesOrderRevisionPricingRequest request)
        {
            if (request == null)
                throw new Exception("Revizyon bilgisi bulunamadı.");

            if (request.Packages == null || request.Packages.Count == 0)
                throw new Exception("Revize edilecek paket bulunamadı.");

            var order = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == request.SalesOrderId);

            if (order == null) throw new Exception("Sipariş bulunamadı.");

            if (order.NetsisTransferredAt == null)
                throw new Exception("Netsis aktarımı yapılmamış siparişte fiyat / miktar revizyonu yapılamaz.");

            if (string.IsNullOrWhiteSpace(order.NetsisOrderNumber))
                throw new Exception("Siparişin Netsis sipariş numarası bulunamadı.");

            var requestPackageIds = request.Packages.Select(x => x.PackageId).ToList();

            if (requestPackageIds.Count != requestPackageIds.Distinct().Count())
                throw new Exception("Aynı paket için birden fazla revizyon gönderilemez.");

            // TÜM PAKETLER

            var packages = await _context.SalesOrderPackages
                .Where(x => x.SalesOrderId == request.SalesOrderId)
                .OrderBy(x => x.PackageNumber).ToListAsync();

            if (packages.Count == 0)
                throw new Exception("Siparişe ait paket bulunamadı.");

            // =========================================================
            // REQUEST'TEKİ PAKETLER BU SİPARİŞE AİT Mİ?
            // =========================================================

            var invalidPackageIds = requestPackageIds
                .Where(id => !packages.Any(x => x.Id == id))
                .ToList();

            if (invalidPackageIds.Count > 0)
                throw new Exception("Siparişe ait olmayan bir paket için revizyon gönderildi.");

            // =========================================================
            // AKTİF PAKETLERİN TAMAMI REQUEST'TE OLMALI
            // IPTAL PAKETLERİ İÇİN FİYAT GEREKMİYOR
            // =========================================================

            var activePackages = packages
                .Where(x => x.Status != "IPTAL")
                .ToList();

            var missingPackages = activePackages
                .Where(x => !requestPackageIds.Contains(x.Id))
                .ToList();

            if (missingPackages.Count > 0)
            {
                throw new Exception(
                    "Tüm aktif paketler için fiyat ve miktar bilgisi gönderilmelidir.");
            }

            // Reçete SP'leri NetOpenX / BOM işlemleri yaptığı için
            // SQL command timeout'u 5 dakikaya çıkarıyoruz.
            _erpContext.Database.SetCommandTimeout(300);

            try
            {
                var now = DateTime.Now;

                // =========================================================
                // 1. GRANITMETAL PAKETLERİNİ GÜNCELLE
                // =========================================================

                foreach (var package in activePackages)
                {
                    var revision = request.Packages
                        .FirstOrDefault(x => x.PackageId == package.Id);

                    if (revision == null)
                    {
                        throw new Exception(
                            $"Paket No={package.PackageNumber} için revizyon bilgisi bulunamadı.");
                    }

                    if (revision.KoliAdedi <= 0)
                    {
                        throw new Exception(
                            $"Paket No={package.PackageNumber} için koli adedi 0'dan büyük olmalıdır.");
                    }

                    if (revision.UnitPrice <= 0)
                    {
                        throw new Exception(
                            $"Paket No={package.PackageNumber} için birim fiyat 0'dan büyük olmalıdır.");
                    }

                    if (package.KoliIciMiktar <= 0)
                    {
                        throw new Exception(
                            $"Paket No={package.PackageNumber} için koli içi miktar geçersiz.");
                    }

                    var packageQuantity =
                        revision.KoliAdedi * package.KoliIciMiktar;

                    var unitKoliPrice =
                        revision.UnitPrice * package.KoliIciMiktar;

                    var totalPrice =
                        packageQuantity * revision.UnitPrice;

                    package.KoliAdedi = revision.KoliAdedi;
                    package.PackageQuantity = packageQuantity;
                    package.UnitPrice = revision.UnitPrice;
                    package.UnitKoliPrice = unitKoliPrice;
                    package.TotalPrice = totalPrice;
                    package.UpdatedAt = now;
                    package.UpdatedBy = _userContext.UserId;
                }

                await _context.SaveChangesAsync();

                // =========================================================
                // 2. TÜM AKTİF KALEMLERİN SHRINK REÇETELERİNİ KONTROL ET
                // =========================================================

                var activePackageIds = activePackages.Select(x => x.Id).ToList();

                var packageLineItems = await _context.SalesOrderPackageLines
                    .Where(x => activePackageIds.Contains(x.SalesOrderPackageId))
                    .Select(x => new
                    {
                        x.SalesOrderPackageId,
                        x.SalesOrderLineId
                    })
                    .ToListAsync();

                var salesOrderLineIds = packageLineItems.Select(x => x.SalesOrderLineId).Distinct().ToList();

                foreach (var salesOrderLineId in salesOrderLineIds)
                {
                    var shrinkRecipeSql = @"
                EXEC dbo.granitSP_SalesOrderCKShrinklenmisReceteOlusturma
                    @SalesOrderLineId";

                    await _erpContext.Database.ExecuteSqlRawAsync(shrinkRecipeSql, new SqlParameter("@SalesOrderLineId", salesOrderLineId));

                }

                // 3. TÜM AKTİF PAKETLERİN PAKET REÇETELERİNİ KONTROL ET
                foreach (var package in activePackages)
                {
                    Console.WriteLine($"PAKET REÇETE BAŞLADI - PackageId: {package.Id}");

                    var packageRecipeSql = @"
                EXEC dbo.granitSP_SalesOrderPackageReceteOlusturma
                    @SalesOrderPackageId";

                    await _erpContext.Database.ExecuteSqlRawAsync(packageRecipeSql, new SqlParameter("@SalesOrderPackageId", package.Id));

                    Console.WriteLine($"PAKET REÇETE BİTTİ - PackageId: {package.Id}");
                }

                // =========================================================
                // 4. NETSİS - GRANITMTL2026
                //
                // TASLAK + Netsis var  -> UPDATE
                // TASLAK + Netsis yok  -> INSERT
                // IPTAL + Netsis var   -> DELETE
                // IPTAL + Netsis yok   -> NOTHING
                // =========================================================

                foreach (var package in packages)
                {
                    var revision = request.Packages
                        .FirstOrDefault(x => x.PackageId == package.Id);

                    await SyncRevisionPackageToNetsisAsync(
                        package,
                        revision,
                        order.NetsisOrderNumber,
                        order.DueDate,
                        false);
                }

                // =========================================================
                // 5. NETSİS - EGEKIRAN2026
                // =========================================================

                foreach (var package in packages)
                {
                    var revision = request.Packages
                        .FirstOrDefault(x => x.PackageId == package.Id);

                    await SyncRevisionPackageToNetsisAsync(
                        package,
                        revision,
                        order.NetsisOrderNumber,
                        order.DueDate,
                        true);
                }

                // 6. SİPARİŞ TAMAMLANDI
                order.Status = "TAMAMLANDI";
                order.UpdatedAt = now;
                order.UpdatedBy = _userContext.UserId;
                await _context.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
        }

        // NETSİS REVİZYON PAKET SENKRONİZASYONU
        private async Task SyncRevisionPackageToNetsisAsync(SalesOrderPackage package,
            SalesOrderPackageRevisionPricingRequest? revision, string netsisOrderNumber, DateTime? dueDate, bool useEgeKiran)
        {
            var tablePrefix = useEgeKiran ? "EGEKIRAN2026.dbo." : "";

            // NETSİS'TE PAKETİ BUL
            var findSql = $@"
                SELECT
                    INCKEYNO,
                    SIRA,
                    STRA_SIPKONT
                FROM {tablePrefix}TBLSIPATRA
                WHERE FISNO = @NetsisOrderNumber
                  AND S_YEDEK2 = @PackageId";

            int? inckeyNo = null;
            short? sira = null;
            short? straSipKont = null;

            var connection = _erpContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = findSql;

                var pOrderNumber = command.CreateParameter();
                pOrderNumber.ParameterName = "@NetsisOrderNumber";
                pOrderNumber.Value = netsisOrderNumber;

                var pPackageId = command.CreateParameter();
                pPackageId.ParameterName = "@PackageId";
                pPackageId.Value = package.Id;

                command.Parameters.Add(pOrderNumber);
                command.Parameters.Add(pPackageId);

                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    inckeyNo = reader.GetInt32(0);
                    if (!reader.IsDBNull(1)) sira = reader.GetInt16(1);
                    if (!reader.IsDBNull(2)) straSipKont = reader.GetInt16(2);
                }
            }

            // IPTAL
            if (package.Status == "IPTAL")
            {
                if (!inckeyNo.HasValue) return;

                // TBLKALEMDETAY
                var deleteKalemDetaySql = $@"
                    DELETE FROM {tablePrefix}TBLKALEMDETAY
                    WHERE REFINCKEYNO = @InckeyNo
                      AND TABLOTIPI = 2";

                await _erpContext.Database.ExecuteSqlRawAsync(deleteKalemDetaySql, new SqlParameter("@InckeyNo", inckeyNo.Value));

                // TBLSSATIRAC
                var deleteSatirAcSql = $@"
                    DELETE FROM {tablePrefix}TBLSSATIRAC
                    WHERE FATNO = @NetsisOrderNumber
                      AND INCKEYNO = @InckeyNo";

                await _erpContext.Database.ExecuteSqlRawAsync(
                    deleteSatirAcSql,
                    new SqlParameter("@NetsisOrderNumber", netsisOrderNumber),
                    new SqlParameter("@InckeyNo", inckeyNo.Value));

                // TBLSIPATRA
                var deleteSipatraSql = $@"
                    DELETE FROM {tablePrefix}TBLSIPATRA
                    WHERE FISNO = @NetsisOrderNumber
                      AND S_YEDEK2 = @PackageId";

                await _erpContext.Database.ExecuteSqlRawAsync(deleteSipatraSql,
                    new SqlParameter("@NetsisOrderNumber", netsisOrderNumber),
                    new SqlParameter("@PackageId", package.Id));
                return;
            }

            // SADECE TASLAK İŞLEMLERİ
            if (package.Status != "TASLAK") return;
            if (revision == null)
            {
                throw new Exception($"Paket No={package.PackageNumber} için revizyon bilgisi bulunamadı.");
            }

            // NETSİS'TE VAR → UPDATE
            if (inckeyNo.HasValue)
            {
                // 1. TBLSIPATRA UPDATE
                var updateSql = $@"
                    UPDATE {tablePrefix}TBLSIPATRA
                    SET
                        STOK_KODU = @StokKodu,
                        STHAR_GCMIK = @KoliAdedi,
                        STHAR_NF = @TotalPrice,
                        STHAR_BF = @TotalPrice,
                        STHAR_DOVFIAT = @UnitKoliPrice,
                        STHAR_TESTAR = @TeslimTarihi
                    WHERE
                        FISNO = @NetsisOrderNumber
                        AND S_YEDEK2 = @PackageId";

                await _erpContext.Database.ExecuteSqlRawAsync(updateSql,
                    new SqlParameter("@StokKodu", package.NetsisPaketKodu ?? (object)DBNull.Value),
                    new SqlParameter("@KoliAdedi", package.KoliAdedi),
                    new SqlParameter("@TotalPrice", package.TotalPrice ?? 0),
                    new SqlParameter("@UnitKoliPrice", package.UnitKoliPrice ?? 0),
                    new SqlParameter("@Aciklama", package.Definition ?? (object)DBNull.Value),
                    new SqlParameter("@TeslimTarihi", dueDate ?? (object)DBNull.Value),
                    new SqlParameter("@NetsisOrderNumber", netsisOrderNumber),
                    new SqlParameter("@PackageId", package.Id));

                // 2. TBLKALEMDETAY ESKİ KAYDI BUL
                short subeKodu = 0;
                short tabloTipi = 2;
                int refInckeyNo = inckeyNo.Value;

                decimal stharDovNf = 0;
                decimal stharCif = 0;

                var kalemDetaySql = $@"
                    SELECT
                        SUBE_KODU,
                        TABLOTIPI,
                        REFINCKEYNO,
                        STHAR_DOVNF,
                        STHAR_CIF
                    FROM {tablePrefix}TBLKALEMDETAY
                    WHERE REFINCKEYNO = @InckeyNo
                      AND TABLOTIPI = 2";

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = kalemDetaySql;

                    var pInckeyNo = command.CreateParameter();
                    pInckeyNo.ParameterName = "@InckeyNo";
                    pInckeyNo.Value = inckeyNo.Value;

                    command.Parameters.Add(pInckeyNo);

                    await using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0)) subeKodu = Convert.ToInt16(reader.GetValue(0));
                        if (!reader.IsDBNull(1)) tabloTipi = Convert.ToInt16(reader.GetValue(1));
                        if (!reader.IsDBNull(2)) refInckeyNo = Convert.ToInt32(reader.GetValue(2));
                        if (!reader.IsDBNull(3)) stharDovNf = Convert.ToDecimal(reader.GetValue(3));
                        if (!reader.IsDBNull(4)) stharCif = Convert.ToDecimal(reader.GetValue(4));
                    }
                }

                // 3. ESKİ TBLKALEMDETAY KAYDINI SİL
                var deleteKalemDetaySql = $@"
                    DELETE FROM {tablePrefix}TBLKALEMDETAY
                    WHERE REFINCKEYNO = @InckeyNo
                      AND TABLOTIPI = 2";

                await _erpContext.Database.ExecuteSqlRawAsync(deleteKalemDetaySql, new SqlParameter("@InckeyNo", inckeyNo.Value));

                // 4. YENİ TBLKALEMDETAY KAYDI EKLE
                var insertKalemDetaySql = $@"
                    INSERT INTO {tablePrefix}TBLKALEMDETAY
                    (
                        SUBE_KODU,
                        TABLOTIPI,
                        REFINCKEYNO,
                        STHAR_DOVNF,
                        STHAR_CIF
                    )
                    VALUES
                    (
                        @SubeKodu,
                        @TabloTipi,
                        @RefInckeyNo,
                        @StharDovNf,
                        @StharCif
                    )";

                await _erpContext.Database.ExecuteSqlRawAsync(
                    insertKalemDetaySql,
                    new SqlParameter("@SubeKodu", subeKodu),
                    new SqlParameter("@TabloTipi", tabloTipi),
                    new SqlParameter("@RefInckeyNo", refInckeyNo),
                    new SqlParameter("@StharDovNf", stharDovNf),
                    new SqlParameter("@StharCif", stharCif));
                return;
            }

            // NETSİS'TE YOK → INSERT
            await InsertRevisionPackageToNetsisAsync(package, netsisOrderNumber, dueDate, useEgeKiran);
        }

        // NETSİS YENİ PAKET SATIRI INSERT
        private async Task InsertRevisionPackageToNetsisAsync(SalesOrderPackage package,
            string netsisOrderNumber, DateTime? dueDate, bool useEgeKiran)
        {
            var tablePrefix = useEgeKiran ? "EGEKIRAN2026.dbo." : "";

            // MEVCUT NETSİS SATIRINI ŞABLON OLARAK AL
            var templateSql = $@"
                SELECT TOP 1
                    CEVRIM,
                    STHAR_GCKOD,
                    STHAR_TARIH,
                    STHAR_KDV,
                    DEPO_KODU,
                    STHAR_SATISK,
                    STHAR_MALFISK,
                    STHAR_FTIRSIP,
                    STHAR_SATISK2,
                    LISTE_FIAT,
                    STHAR_HTUR,
                    STHAR_DOVTIP,
                    PROMASYON_KODU,
                    STHAR_ODEGUN,
                    STRA_SATISK3,
                    STRA_SATISK4,
                    STRA_SATISK5,
                    STRA_SATISK6,
                    STHAR_BGTIP,
                    STHAR_KOD1,
                    STHAR_KOD2,
                    STHAR_CARIKOD,
                    STHAR_SIP_TURU,
                    PLASIYER_KODU,
                    EKALAN_NEDEN,
                    EKALAN,
                    REDMIK,
                    REDNEDEN,
                    AMBAR_KABULNO,
                    FIRMA_DOVTIP,
                    FIRMA_DOVTUT,
                    FIRMA_DOVMAL,
                    UPDATE_KODU,
                    IRSALIYE_NO,
                    IRSALIYE_TARIH,
                    KOSULKODU,
                    ECZA_FAT_TIP,
                    OLCUBR,
                    VADE_TARIHI,
                    LISTE_NO,
                    BAGLANTI_NO,
                    SUBE_KODU,
                    MUH_KODU,
                    S_YEDEK1,
                    F_YEDEK3,
                    F_YEDEK4,
                    F_YEDEK5,
                    C_YEDEK6,
                    B_YEDEK7,
                    I_YEDEK8,
                    L_YEDEK9,
                    PROJE_KODU,
                    KOSULTARIHI,
                    SATISK1TIP,
                    SATISK2TIP,
                    SATISK3TIP,
                    SATISK4TIP,
                    SATISK5TIP,
                    SATISK6TIP,
                    EXPORTTYPE,
                    EXPORTMIK,
                    ONAYTIPI,
                    ONAYNUM,
                    KKMALF,
                    STRA_IRSKONT,
                    YAPKOD,
                    MAMYAPKOD,
                    OTVFIYAT,
                    IRS_INCKEYNO
                FROM {tablePrefix}TBLSIPATRA
                WHERE FISNO = @NetsisOrderNumber
                ORDER BY SIRA";

            var templateValues = new Dictionary<string, object?>();

            await using (var command = _erpContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = templateSql;

                var p = command.CreateParameter();
                p.ParameterName = "@NetsisOrderNumber";
                p.Value = netsisOrderNumber;

                command.Parameters.Add(p);

                var connection = command.Connection!;
                if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

                await using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new Exception($"Netsis siparişi için şablon satır bulunamadı. Sipariş: {netsisOrderNumber}");
                }

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    templateValues[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }
            // YENİ SIRA

            var nextSiraSql = $@"
                SELECT
                    ISNULL(MAX(SIRA), 0) + 1
                FROM {tablePrefix}TBLSIPATRA
                WHERE FISNO = @NetsisOrderNumber";

            var nextSipKontSql = $@"
                SELECT
                    ISNULL(MAX(STRA_SIPKONT), 0) + 1
                FROM {tablePrefix}TBLSIPATRA
                WHERE FISNO = @NetsisOrderNumber";

            short nextSira;
            short nextSipKont;

            await using (var command = _erpContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = nextSiraSql;

                var p = command.CreateParameter();
                p.ParameterName = "@NetsisOrderNumber";
                p.Value = netsisOrderNumber;
                command.Parameters.Add(p);
                var result = await command.ExecuteScalarAsync();
                nextSira = Convert.ToInt16(result);
            }

            await using (var command = _erpContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = nextSipKontSql;

                var p = command.CreateParameter();
                p.ParameterName = "@NetsisOrderNumber";
                p.Value = netsisOrderNumber;

                command.Parameters.Add(p);
                var result = await command.ExecuteScalarAsync();
                nextSipKont = Convert.ToInt16(result);
            }
            // PAKET HESAPLARI

            var dovizTip = Convert.ToInt32(templateValues["STHAR_DOVTIP"] ?? 0);
            var totalPrice = package.TotalPrice ?? 0;
            var unitKoliPrice = package.UnitKoliPrice ?? 0;
            decimal netsisNf = totalPrice;

            if (dovizTip != 0)
            {
                var dovizSql = @"
                    SELECT TOP 1 DOV_SATIS
                    FROM NETSIS..DOVIZ
                    WHERE SIRA = @DovizTip
                    ORDER BY TARIH DESC";

                await using var command = _erpContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = dovizSql;

                var p = command.CreateParameter();
                p.ParameterName = "@DovizTip";
                p.Value = dovizTip;

                command.Parameters.Add(p);
                var result = await command.ExecuteScalarAsync();
                var doviz = result == null || result == DBNull.Value ? 1m : Convert.ToDecimal(result);
                netsisNf = totalPrice * doviz;
            }

            // TBLSIPATRA INSERT
            var insertSql = $@"
                DECLARE @NewKeys TABLE
                (
                    INCKEYNO INT
                );

                INSERT INTO {tablePrefix}TBLSIPATRA
                (
                    STOK_KODU,
                    FISNO,
                    STHAR_GCMIK,
                    STHAR_GCMIK2,
                    CEVRIM,
                    STHAR_GCKOD,
                    STHAR_TARIH,
                    STHAR_NF,
                    STHAR_BF,
                    STHAR_IAF,
                    STHAR_KDV,
                    DEPO_KODU,
                    STHAR_ACIKLAMA,
                    STHAR_SATISK,
                    STHAR_MALFISK,
                    STHAR_FTIRSIP,
                    STHAR_SATISK2,
                    LISTE_FIAT,
                    STHAR_HTUR,
                    STHAR_DOVTIP,
                    PROMASYON_KODU,
                    STHAR_DOVFIAT,
                    STHAR_ODEGUN,
                    STRA_SATISK3,
                    STRA_SATISK4,
                    STRA_SATISK5,
                    STRA_SATISK6,
                    STHAR_BGTIP,
                    STHAR_KOD1,
                    STHAR_KOD2,
                    STHAR_SIPNUM,
                    STHAR_CARIKOD,
                    STHAR_SIP_TURU,
                    PLASIYER_KODU,
                    EKALAN_NEDEN,
                    EKALAN,
                    EKALAN1,
                    REDMIK,
                    REDNEDEN,
                    SIRA,
                    STRA_SIPKONT,
                    AMBAR_KABULNO,
                    FIRMA_DOVTIP,
                    FIRMA_DOVTUT,
                    FIRMA_DOVMAL,
                    UPDATE_KODU,
                    IRSALIYE_NO,
                    IRSALIYE_TARIH,
                    KOSULKODU,
                    ECZA_FAT_TIP,
                    STHAR_TESTAR,
                    OLCUBR,
                    VADE_TARIHI,
                    LISTE_NO,
                    BAGLANTI_NO,
                    SUBE_KODU,
                    MUH_KODU,
                    S_YEDEK1,
                    S_YEDEK2,
                    F_YEDEK3,
                    F_YEDEK4,
                    F_YEDEK5,
                    C_YEDEK6,
                    B_YEDEK7,
                    I_YEDEK8,
                    L_YEDEK9,
                    D_YEDEK10,
                    PROJE_KODU,
                    FIYATTARIHI,
                    KOSULTARIHI,
                    SATISK1TIP,
                    SATISK2TIP,
                    SATISK3TIP,
                    SATISK4TIP,
                    SATISK5TIP,
                    SATISK6TIP,
                    EXPORTTYPE,
                    EXPORTMIK,
                    ONAYTIPI,
                    ONAYNUM,
                    KKMALF,
                    STRA_IRSKONT,
                    YAPKOD,
                    MAMYAPKOD,
                    OTVFIYAT,
                    IRS_INCKEYNO
                )
                OUTPUT INSERTED.INCKEYNO INTO @NewKeys(INCKEYNO)
                VALUES
                (
                    @StokKodu,
                    @Fisno,
                    @KoliAdedi,
                    0,
                    @Cevrim,
                    @Gckod,
                    @Tarih,
                    @Nf,
                    @Nf,
                    0,
                    @Kdv,
                    @DepoKodu,
                    @CariKod,
                    @Satisk,
                    @Malfisk,
                    @Ftirsip,
                    @Satisk2,
                    @ListeFiat,
                    @Htur,
                    @Dovtip,
                    @Promosyon,
                    @DovFiyat,
                    @Odegun,
                    @Satisk3,
                    @Satisk4,
                    @Satisk5,
                    @Satisk6,
                    @Bgtip,
                    @Kod1,
                    @Kod2,
                    NULL,
                    @CariKod,
                    @SipTuru,
                    @Plasiyer,
                    @EkalanNeden,
                    @Ekalan,
                    @Ekalan1,
                    @Redmik,
                    @Redneden,
                    @Sira,
                    @SipKont,
                    @Ambar,
                    @FirmaDovtip,
                    @FirmaDovtut,
                    @FirmaDovmal,
                    @UpdateKodu,
                    @IrsaliyeNo,
                    @IrsaliyeTarih,
                    @KosulKodu,
                    @EczaFatTip,
                    @TeslimTarihi,
                    @Olcubr,
                    @VadeTarihi,
                    @ListeNo,
                    @BaglantiNo,
                    @SubeKodu,
                    @MuhKodu,
                    @SYedek1,
                    @PackageId,
                    @FYedek3,
                    @FYedek4,
                    @FYedek5,
                    @CYedek6,
                    @BYedek7,
                    @IYedek8,
                    @LYedek9,
                    GETDATE(),
                    @ProjeKodu,
                    GETDATE(),
                    @KosulTarihi,
                    @Satisk1Tip,
                    @Satisk2Tip,
                    @Satisk3Tip,
                    @Satisk4Tip,
                    @Satisk5Tip,
                    @Satisk6Tip,
                    @ExportType,
                    @ExportMik,
                    @OnayTipi,
                    @OnayNum,
                    @Kkmalf,
                    @StraIrskont,
                    @YapKod,
                    @MamYapKod,
                    @OtvFiyat,
                    @IrsInckey
                );

                SELECT INCKEYNO
                FROM @NewKeys;
            ";

            int newInckeyNo;
            await using (var command = _erpContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = insertSql;

                AddParameter(command, "@StokKodu", package.NetsisPaketKodu);
                AddParameter(command, "@Fisno", netsisOrderNumber);
                AddParameter(command, "@KoliAdedi", package.KoliAdedi);
                AddParameter(command, "@Cevrim", package.KoliIciMiktar);
                AddParameter(command, "@Gckod", templateValues["STHAR_GCKOD"]);
                AddParameter(command, "@Tarih", templateValues["STHAR_TARIH"]);
                AddParameter(command, "@Nf", netsisNf);
                AddParameter(command, "@Kdv", templateValues["STHAR_KDV"]);
                AddParameter(command, "@DepoKodu", templateValues["DEPO_KODU"]);
                AddParameter(command, "@Aciklama", package.Definition ?? "");
                AddParameter(command, "@Satisk", templateValues["STHAR_SATISK"]);
                AddParameter(command, "@Malfisk", templateValues["STHAR_MALFISK"]);
                AddParameter(command, "@Ftirsip", templateValues["STHAR_FTIRSIP"]);
                AddParameter(command, "@Satisk2", templateValues["STHAR_SATISK2"]);
                AddParameter(command, "@ListeFiat", templateValues["LISTE_FIAT"]);
                AddParameter(command, "@Htur", templateValues["STHAR_HTUR"]);
                AddParameter(command, "@Dovtip", templateValues["STHAR_DOVTIP"]);
                AddParameter(command, "@Promosyon", templateValues["PROMASYON_KODU"]);
                AddParameter(command, "@DovFiyat", unitKoliPrice);
                AddParameter(command, "@Odegun", templateValues["STHAR_ODEGUN"]);
                AddParameter(command, "@Satisk3", templateValues["STRA_SATISK3"]);
                AddParameter(command, "@Satisk4", templateValues["STRA_SATISK4"]);
                AddParameter(command, "@Satisk5", templateValues["STRA_SATISK5"]);
                AddParameter(command, "@Satisk6", templateValues["STRA_SATISK6"]);
                AddParameter(command, "@Bgtip", templateValues["STHAR_BGTIP"]);
                AddParameter(command, "@Kod1", templateValues["STHAR_KOD1"]);
                AddParameter(command, "@Kod2", templateValues["STHAR_KOD2"]);
                AddParameter(command, "@CariKod", templateValues["STHAR_CARIKOD"]);
                AddParameter(command, "@SipTuru", templateValues["STHAR_SIP_TURU"]);
                AddParameter(command, "@Plasiyer", templateValues["PLASIYER_KODU"]);
                AddParameter(command, "@EkalanNeden", templateValues["EKALAN_NEDEN"]);
                AddParameter(command, "@Ekalan", templateValues["EKALAN"]);
                AddParameter(command, "@Ekalan1", package.Definition ?? "");
                AddParameter(command, "@Redmik", templateValues["REDMIK"]);
                AddParameter(command, "@Redneden", templateValues["REDNEDEN"]);
                AddParameter(command, "@Sira", nextSira);
                AddParameter(command, "@SipKont", nextSipKont);
                AddParameter(command, "@Ambar", templateValues["AMBAR_KABULNO"]);
                AddParameter(command, "@FirmaDovtip", templateValues["FIRMA_DOVTIP"]);
                AddParameter(command, "@FirmaDovtut", templateValues["FIRMA_DOVTUT"]);
                AddParameter(command, "@FirmaDovmal", templateValues["FIRMA_DOVMAL"]);
                AddParameter(command, "@UpdateKodu", templateValues["UPDATE_KODU"]);
                AddParameter(command, "@IrsaliyeNo", templateValues["IRSALIYE_NO"]);
                AddParameter(command, "@IrsaliyeTarih", templateValues["IRSALIYE_TARIH"]);
                AddParameter(command, "@KosulKodu", templateValues["KOSULKODU"]);
                AddParameter(command, "@EczaFatTip", templateValues["ECZA_FAT_TIP"]);
                AddParameter(command, "@TeslimTarihi", dueDate);
                AddParameter(command, "@Olcubr", templateValues["OLCUBR"]);
                AddParameter(command, "@VadeTarihi", dueDate);
                AddParameter(command, "@ListeNo", templateValues["LISTE_NO"]);
                AddParameter(command, "@BaglantiNo", templateValues["BAGLANTI_NO"]);
                AddParameter(command, "@SubeKodu", templateValues["SUBE_KODU"]);
                AddParameter(command, "@MuhKodu", templateValues["MUH_KODU"]);
                AddParameter(command, "@SYedek1", templateValues["S_YEDEK1"]);
                AddParameter(command, "@PackageId", package.Id);
                AddParameter(command, "@FYedek3", templateValues["F_YEDEK3"]);
                AddParameter(command, "@FYedek4", templateValues["F_YEDEK4"]);
                AddParameter(command, "@FYedek5", templateValues["F_YEDEK5"]);
                AddParameter(command, "@CYedek6", templateValues["C_YEDEK6"]);
                AddParameter(command, "@BYedek7", templateValues["B_YEDEK7"]);
                AddParameter(command, "@IYedek8", templateValues["I_YEDEK8"]);
                AddParameter(command, "@LYedek9", templateValues["L_YEDEK9"]);
                AddParameter(command, "@ProjeKodu", templateValues["PROJE_KODU"]);
                AddParameter(command, "@KosulTarihi", templateValues["KOSULTARIHI"]);
                AddParameter(command, "@Satisk1Tip", templateValues["SATISK1TIP"]);
                AddParameter(command, "@Satisk2Tip", templateValues["SATISK2TIP"]);
                AddParameter(command, "@Satisk3Tip", templateValues["SATISK3TIP"]);
                AddParameter(command, "@Satisk4Tip", templateValues["SATISK4TIP"]);
                AddParameter(command, "@Satisk5Tip", templateValues["SATISK5TIP"]);
                AddParameter(command, "@Satisk6Tip", templateValues["SATISK6TIP"]);
                AddParameter(command, "@ExportType", templateValues["EXPORTTYPE"]);
                AddParameter(command, "@ExportMik", templateValues["EXPORTMIK"]);
                AddParameter(command, "@OnayTipi", templateValues["ONAYTIPI"]);
                AddParameter(command, "@OnayNum", templateValues["ONAYNUM"]);
                AddParameter(command, "@Kkmalf", templateValues["KKMALF"]);
                AddParameter(command, "@StraIrskont", templateValues["STRA_IRSKONT"]);
                AddParameter(command, "@YapKod", templateValues["YAPKOD"]);
                AddParameter(command, "@MamYapKod", templateValues["MAMYAPKOD"]);
                AddParameter(command, "@OtvFiyat", templateValues["OTVFIYAT"]);
                AddParameter(command, "@IrsInckey", templateValues["IRS_INCKEYNO"]);

                var connection = command.Connection!;
                if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Netsis TBLSIPATRA INSERT başarısız oldu. Paket: {package.Id}");
                }
                newInckeyNo = Convert.ToInt32(result);
            }

            // TBLSSATIRAC INSERT
            var insertSatirAcSql = $@"
                INSERT INTO {tablePrefix}TBLSSATIRAC
                (
                    SUBE_KODU,
                    BGTIP,
                    FATNO,
                    CKOD,
                    SIRANO,
                    INCKEYNO,
                    ONAYTIPI,
                    ONAYNUM
                )
                VALUES
                (
                    @SubeKodu,
                    @Bgtip,
                    @Fatno,
                    @Ckod,
                    @Sirano,
                    @InckeyNo,
                    @OnayTipi,
                    @OnayNum
                )";

            await _erpContext.Database.ExecuteSqlRawAsync(insertSatirAcSql,
                new SqlParameter("@SubeKodu", templateValues["SUBE_KODU"] ?? 0),
                new SqlParameter("@Bgtip", templateValues["STHAR_BGTIP"] ?? 6),
                new SqlParameter("@Fatno", netsisOrderNumber),
                new SqlParameter("@Ckod", templateValues["STHAR_CARIKOD"] ?? ""),
                new SqlParameter("@Sirano", nextSira),
                new SqlParameter("@InckeyNo", newInckeyNo),
                new SqlParameter("@OnayTipi", templateValues["ONAYTIPI"] ?? "A"),
                new SqlParameter("@OnayNum", templateValues["ONAYNUM"] ?? 0));
        }

        // PARAMETRE YARDIMCISI
        private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}