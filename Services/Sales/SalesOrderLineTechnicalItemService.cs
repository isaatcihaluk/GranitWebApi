using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.NETSISMODELLER;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderLineTechnicalItemService : ISalesOrderLineTechnicalItemService
    {
        private readonly AppDbContext _context;
        private readonly ErpDbContext _erpContext;
        private readonly UserContext _userContext;

        public SalesOrderLineTechnicalItemService(AppDbContext context, ErpDbContext erpContext, UserContext userContext)
        {
            _context = context;
            _erpContext = erpContext;
            _userContext = userContext;
        }

        public async Task<List<SalesOrderLineTechnicalItem>> GetBySalesOrderLineIdAsync(long salesOrderLineId)
        {
            return await _context.SalesOrderLineTechnicalItems
                .Include(x => x.Image).ThenInclude(x => x!.ImageType)
                .Where(x => x.SalesOrderLineId == salesOrderLineId && x.IsActive)
                .OrderByDescending(x => x.SelectedAt).ToListAsync();
        }

        public async Task<List<SalesOrderLineTechnicalItem>> GetByImageIdAsync(long imageId)
        {
            return await _context.SalesOrderLineTechnicalItems
                .Where(x => x.ImageId == imageId && x.IsActive)
                .OrderByDescending(x => x.SelectedAt)
                .ToListAsync();
        }

        public async Task<SalesOrderLineTechnicalItem?> GetByIdAsync(long id)
        {
            return await _context.SalesOrderLineTechnicalItems
                .Include(x => x.Image).ThenInclude(x => x!.ImageType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<SalesOrderLineTechnicalItem> CreateAsync(SalesOrderLineTechnicalItem technicalItem)
        {
            var lineExists = await _context.SalesOrderLines.AnyAsync(x => x.Id == technicalItem.SalesOrderLineId);

            if (!lineExists)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            var image = await _context.SalesOrderLineImages.FirstOrDefaultAsync(x => x.Id == technicalItem.ImageId && x.IsActive);

            if (image == null)
            {
                throw new Exception("Görsel bulunamadı veya aktif durumda değil.");
            }

            if (image.SalesOrderLineId != technicalItem.SalesOrderLineId)
            {
                throw new Exception("Seçilen görsel ilgili sipariş satırına ait değil.");
            }

            technicalItem.Id = 0;
            technicalItem.IsActive = true;
            technicalItem.SelectedAt = DateTime.Now;

            _context.SalesOrderLineTechnicalItems.Add(technicalItem);
            await _context.SaveChangesAsync();
            return technicalItem;
        }

        public async Task<SalesOrderLineTechnicalItem?> UpdateAsync(long id, SalesOrderLineTechnicalItem technicalItem)
        {
            var existing = await _context.SalesOrderLineTechnicalItems.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null) { return null; }

            var image = await _context.SalesOrderLineImages.FirstOrDefaultAsync(x => x.Id == technicalItem.ImageId && x.IsActive);

            if (image == null)
            {
                throw new Exception("Görsel bulunamadı veya aktif durumda değil.");
            }

            if (image.SalesOrderLineId != existing.SalesOrderLineId)
            {
                throw new Exception("Seçilen görsel ilgili sipariş satırına ait değil.");
            }

            existing.ImageId = technicalItem.ImageId;
            existing.StockCode = technicalItem.StockCode;
            existing.StockName = technicalItem.StockName;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = technicalItem.UpdatedBy;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.SalesOrderLineTechnicalItems.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) { return false; }
            existing.IsActive = false;
            existing.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TBLSTSABIT>> GetStockInfoAsync()
        {
            return await _erpContext.TBLSTSABIT.AsNoTracking()
            .Where(x => x.STOK_KODU.StartsWith("3202") || x.STOK_KODU.StartsWith("3203") || x.STOK_KODU.StartsWith("3204"))
            .OrderBy(x => x.STOK_KODU).ToListAsync();
        }

        public async Task SaveTechnicalItemsAsync(
    long salesOrderLineId,
    List<SalesOrderLineTechnicalItem> technicalItems)
        {
            var lineExists = await _context.SalesOrderLines
                .AnyAsync(x => x.Id == salesOrderLineId);

            if (!lineExists)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            var userId = _userContext.UserId;

            var line = await _context.SalesOrderLines
                .FirstOrDefaultAsync(x => x.Id == salesOrderLineId);

            if (line == null)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            var isYK = line.ProductGroupId == "5";

            foreach (var technicalItem in technicalItems)
            {
                if (technicalItem.ImageId <= 0)
                {
                    throw new Exception("Geçersiz görsel bilgisi.");
                }

                // Tüm ürün gruplarında seçilen görsel için
                // stok seçilmiş olması gerekiyor.
                if (string.IsNullOrWhiteSpace(technicalItem.StockCode))
                {
                    throw new Exception(
                        $"ImageId={technicalItem.ImageId} için stok kodu seçilmemiş.");
                }

                var image = await _context.SalesOrderLineImages
                    .FirstOrDefaultAsync(x =>
                        x.Id == technicalItem.ImageId &&
                        x.IsActive);

                if (image == null)
                {
                    throw new Exception(
                        $"ImageId={technicalItem.ImageId} olan görsel bulunamadı veya aktif değil.");
                }

                if (image.SalesOrderLineId != salesOrderLineId)
                {
                    throw new Exception(
                        "Seçilen görsel ilgili sipariş satırına ait değil.");
                }

                var existing = await _context.SalesOrderLineTechnicalItems
                    .FirstOrDefaultAsync(x =>
                        x.SalesOrderLineId == salesOrderLineId &&
                        x.ImageId == technicalItem.ImageId &&
                        x.IsActive);

                if (existing == null)
                {
                    var newItem = new SalesOrderLineTechnicalItem
                    {
                        SalesOrderLineId = salesOrderLineId,
                        ImageId = technicalItem.ImageId,
                        StockCode = technicalItem.StockCode,
                        StockName = technicalItem.StockName,
                        SelectedAt = DateTime.Now,
                        SelectedBy = userId,
                        IsActive = true
                    };

                    _context.SalesOrderLineTechnicalItems.Add(newItem);
                }
                else
                {
                    existing.StockCode = technicalItem.StockCode;
                    existing.StockName = technicalItem.StockName;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = userId;
                }
            }
            await _context.SaveChangesAsync();
            if (isYK)
            {
                line.ShrinkliKod = line.CatalogCode;
                line.ShrinkliAd = line.ProductName;
            }
            else
            {
                await GenerateShrinkliKodAsync(salesOrderLineId);
                await GenerateShrinkliAdAsync(salesOrderLineId);
            }

            await _context.SaveChangesAsync();
        }
        private async Task GenerateShrinkliKodAsync(long salesOrderLineId)
        {
            var line = await _context.SalesOrderLines.Include(x => x.CKConfiguration)
                .Include(x => x.UMConfiguration).FirstOrDefaultAsync(x => x.Id == salesOrderLineId);

            if (line == null) throw new Exception("Sipariş satırı bulunamadı.");

            if (string.IsNullOrWhiteSpace(line.MainAssemblyCode)) throw new Exception("Sipariş satırında Ana Montaj Kodu bulunamadı.");

            // CK
            if (line.ProductGroupId == "1")
            {
                if (!line.MainAssemblyCode.StartsWith("2190"))
                {
                    throw new Exception($"Shrinkli kod oluşturulamadı. Ana Montaj Kodu CK formatında değil: {line.MainAssemblyCode}");
                }

                // 2190 -> 2100
                var shrinkliAnaMontajKodu = "2100" + line.MainAssemblyCode.Substring(4);
                // Varsayılan: Broşür yok
                var lastThreeChars = "000";
                // ImageTypeId = 1 -> Broşür görseli
                var brosurImage = await _context.SalesOrderLineImages
                    .FirstOrDefaultAsync(x =>x.SalesOrderLineId == salesOrderLineId &&x.ImageTypeId == 1 && x.IsActive);

                // Broşür görseli varsa teknik stok kontrol edilir
                if (brosurImage != null)
                {
                    var technicalItem = await _context.SalesOrderLineTechnicalItems
                        .FirstOrDefaultAsync(x =>
                            x.SalesOrderLineId == salesOrderLineId &&
                            x.ImageId == brosurImage.Id &&
                            x.IsActive);

                    // Teknik stok seçilmişse son 3 karakter alınır
                    if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockCode))
                    {
                        lastThreeChars = technicalItem.StockCode.Length >= 3
                                ? technicalItem.StockCode[^3..] : technicalItem.StockCode.PadLeft(3, '0');
                    }
                }
                line.ShrinkliKod = $"{shrinkliAnaMontajKodu}.{lastThreeChars}";
            }

            // UM
            else if (line.ProductGroupId == "2")
            {
                var configuration = line.UMConfiguration;
                if (configuration == null)
                {
                    throw new Exception($"Satış satırı için UM konfigürasyonu bulunamadı. LineId: {salesOrderLineId}");
                }

                if (!line.MainAssemblyCode.StartsWith("2290"))
                {
                    throw new Exception($"Shrinkli kod oluşturulamadı. Ana Montaj Kodu UM formatında değil: {line.MainAssemblyCode}");
                }
                if (string.IsNullOrWhiteSpace(configuration.FabricName))
                {
                    throw new Exception(
                        $"Satır {line.LineNumber} için kumaş adı bulunamadı.");
                }
                if (string.IsNullOrWhiteSpace(configuration.SpongeName))
                {
                    throw new Exception($"Satır {line.LineNumber} için sünger adı bulunamadı.");
                }

                // 2290 -> 2200
                var shrinkliAnaMontajKodu = "2200" + line.MainAssemblyCode.Substring(4);

                // FabricName son 4 karakter
                // Örn: K3390 -> 3390
                var fabricName = configuration.FabricName.Trim();
                var fabricLastFour = fabricName.Length >= 4 ? fabricName[^4..] : fabricName.PadLeft(4, '0');
                var spongeName = configuration.SpongeName.Trim();
                var lastThreeChars = "000";

                var brosurImage = await _context.SalesOrderLineImages
                    .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageTypeId == 1 && x.IsActive);

                // Broşür varsa teknik stok kontrol edilir
                if (brosurImage != null)
                {
                    var technicalItem = await _context.SalesOrderLineTechnicalItems
                        .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageId == brosurImage.Id && x.IsActive);

                    if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockCode))
                    {
                        lastThreeChars = technicalItem.StockCode.Length >= 3
                                ? technicalItem.StockCode[^3..] : technicalItem.StockCode.PadLeft(3, '0');
                    }
                }
                // UM Shrinkli kod
                line.ShrinkliKod = $"{shrinkliAnaMontajKodu}.{fabricLastFour}.{spongeName}.{lastThreeChars}";
            }
            else
            {
                throw new Exception(
                    $"Shrinkli kod oluşturma işlemi desteklenmeyen ürün grubu için yapılamaz. " +
                    $"ProductGroupId: {line.ProductGroupId}");
            }
            line.UpdatedAt = DateTime.Now;
            line.UpdatedBy = _userContext.UserId;
        }
        private async Task GenerateShrinkliAdAsync(long salesOrderLineId)
        {
            var line = await _context.SalesOrderLines.Include(x => x.CKConfiguration)
                .Include(x => x.UMConfiguration).FirstOrDefaultAsync(x => x.Id == salesOrderLineId);

            if (line == null) throw new Exception("Sipariş satırı bulunamadı.");

            // CK
            if (line.ProductGroupId == "1")
            {
                if (line.CKConfiguration == null) throw new Exception("CK konfigürasyonu bulunamadı.");

                var configuration = line.CKConfiguration;
                var urunGrubuKisaltmasi = await _context.Database
                    .SqlQueryRaw<string?>(
                        @"SELECT TOP 1 KISATLMASI AS Value
                          FROM GRANIT_TBL_URUN_GRUBU
                          WHERE ID = {0}",
                        line.ProductGroupId)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(urunGrubuKisaltmasi))
                {
                    throw new Exception($"ProductGroupId={line.ProductGroupId} için ürün grubu kısaltması bulunamadı.");
                }

                var footRalAdi = await _context.Database
                    .SqlQueryRaw<string?>(
                        @"SELECT TOP 1 AYAK_RAL_DETAY AS Value
                          FROM GRANIT_TBL_CK_URUN_DETAY_RENK
                          WHERE AYAK_RAL = {0}",
                        configuration.FootRal)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(footRalAdi))
                {
                    throw new Exception($"FootRal={configuration.FootRal} için RAL açıklaması bulunamadı.");
                }

                var bodyWingRalAdi = await _context.Database
                    .SqlQueryRaw<string?>(
                        @"SELECT TOP 1 GOVDE_KANAT_RAL_DETAY AS Value
                          FROM GRANIT_TBL_CK_URUN_DETAY_RENK
                          WHERE GOVDE_KANAT_RAL = {0}",
                        configuration.BodyWingRal)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(bodyWingRalAdi))
                {
                    throw new Exception($"BodyWingRal={configuration.BodyWingRal} için RAL açıklaması bulunamadı.");
                }

                var plastik1 = await _context.Database
                    .SqlQueryRaw<string?>(
                        @"SELECT TOP 1 RENK AS Value
                          FROM GRANIT_TBL_PLASTIK_RENK
                          WHERE NO = {0}",
                        configuration.PlasticColor1No)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(plastik1))
                {
                    throw new Exception($"PlasticColor1No={configuration.PlasticColor1No} için plastik rengi bulunamadı.");
                }

                var plastik2 = await _context.Database
                    .SqlQueryRaw<string?>(
                        @"SELECT TOP 1 RENK AS Value
                          FROM GRANIT_TBL_PLASTIK_RENK
                          WHERE NO = {0}",
                        configuration.PlasticColor2No)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(plastik2))
                {
                    throw new Exception($"PlasticColor2No={configuration.PlasticColor2No} için plastik rengi bulunamadı.");
                }

                var lastThreeChars = "000";
                var brosurSuffix = "BROSURSUZ";
                var brosurImage = await _context.SalesOrderLineImages
                    .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageTypeId == 1 && x.IsActive);

                if (brosurImage != null)
                {
                    var technicalItem = await _context.SalesOrderLineTechnicalItems
                        .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageId == brosurImage.Id && x.IsActive);

                    if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockCode))
                    {
                        lastThreeChars = technicalItem.StockCode.Length >= 3
                                ? technicalItem.StockCode[^3..] : technicalItem.StockCode.PadLeft(3, '0');

                        // Diğer -> StockName içerisindeki parantez değeri
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
                            if (!string.IsNullOrWhiteSpace(technicalItem.StockName))
                            {
                                var match = Regex.Match(technicalItem.StockName,@"\(([^()]*)\)");
                                if (match.Success)
                                {
                                    brosurSuffix = match.Groups[1].Value.Trim();
                                }
                                else
                                {
                                    throw new Exception(
                                        "Shrinkli isim oluşturulamadı. " +
                                        "Broşür stok adında parantez içerisinde referans/barkod bulunamadı.");
                                }
                            }
                            else
                            {
                                throw new Exception(
                                    "Shrinkli isim oluşturulamadı. Broşür stok adı bulunamadı.");
                            }
                        }
                    }
                }

                var ralText = configuration.FootRal == configuration.BodyWingRal
                        ? footRalAdi : $"{footRalAdi}/{bodyWingRalAdi}";

                var plastikText = configuration.PlasticColor1No == configuration.PlasticColor2No
                        ? plastik1 : $"{plastik1}/{plastik2}";

                line.ShrinkliAd =
                    $"{urunGrubuKisaltmasi.Trim()} SHRINKLI " +
                    $"{line.ProductName?.Trim()} " +
                    $"RAL {ralText.Trim()} " +
                    $"PLSTK {plastikText.Trim()} " +
                    $"{brosurSuffix.Trim()}";
            }

            // UM
            else if (line.ProductGroupId == "2")
            {
                var configuration = line.UMConfiguration;
                if (configuration == null)
                {
                    throw new Exception($"Satış satırı için UM konfigürasyonu bulunamadı. LineId: {salesOrderLineId}");
                }
                if (string.IsNullOrWhiteSpace(configuration.BodyIroningRal))
                {
                    throw new Exception($"Satır {line.LineNumber} için Gövde/Ütü Masası RAL bilgisi bulunamadı.");
                }
                if (string.IsNullOrWhiteSpace(configuration.BodyIroningColor))
                {
                    throw new Exception($"Satır {line.LineNumber} için Gövde/Ütü Masası renk bilgisi bulunamadı.");
                }
                if (string.IsNullOrWhiteSpace(configuration.PlasticCombinationDescription))
                {
                    throw new Exception($"Satır {line.LineNumber} için plastik kombinasyonu bulunamadı.");
                }
                if (string.IsNullOrWhiteSpace(configuration.FabricName))
                {
                    throw new Exception($"Satır {line.LineNumber} için kumaş adı bulunamadı.");
                }
                if (string.IsNullOrWhiteSpace(configuration.SpongeName))
                {
                    throw new Exception($"Satır {line.LineNumber} için sünger adı bulunamadı.");
                }
                // Varsayılan olarak broşür yok
                var lastThreeChars = "000";
                var brosurSuffix = "BROSURSUZ";
                var brosurImage = await _context.SalesOrderLineImages
                    .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageTypeId == 1 && x.IsActive);

                if (brosurImage != null)
                {
                    var technicalItem = await _context.SalesOrderLineTechnicalItems
                        .FirstOrDefaultAsync(x => x.SalesOrderLineId == salesOrderLineId && x.ImageId == brosurImage.Id && x.IsActive);

                    if (technicalItem != null && !string.IsNullOrWhiteSpace(technicalItem.StockCode))
                    {
                        lastThreeChars = technicalItem.StockCode.Length >= 3
                                ? technicalItem.StockCode[^3..]: technicalItem.StockCode.PadLeft(3, '0');

                        // Diğer -> StockName içerisindeki parantez değeri
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
                            if (!string.IsNullOrWhiteSpace(technicalItem.StockName))
                            {
                                var match = Regex.Match(technicalItem.StockName,@"\(([^()]*)\)");

                                if (match.Success)
                                {
                                    brosurSuffix = match.Groups[1].Value.Trim();
                                }
                                else
                                {
                                    throw new Exception(
                                        "Shrinkli isim oluşturulamadı. " +
                                        "Broşür stok adında parantez içerisinde referans/barkod bulunamadı.");
                                }
                            }
                            else
                            {
                                throw new Exception(
                                    "Shrinkli isim oluşturulamadı. Broşür stok adı bulunamadı.");
                            }
                        }
                    }
                }
                // UM Shrinkli Ad
                line.ShrinkliAd =
                    $"UM SHRINKLI " +
                    $"{line.ProductName?.Trim()} " +
                    $"RAL {configuration.BodyIroningRal.Trim()} " +
                    $"{configuration.BodyIroningColor.Trim()} " +
                    $"- PLSTK {configuration.PlasticCombinationDescription.Trim()} " +
                    $"{configuration.FabricName.Trim()} " +
                    $"{configuration.SpongeName.Trim()} MM " +
                    $"{brosurSuffix.Trim()}";
            }
            else
            {
                throw new Exception(
                    $"Shrinkli ad oluşturma işlemi desteklenmeyen ürün grubu için yapılamaz. " +
                    $"ProductGroupId: {line.ProductGroupId}");
            }
            line.UpdatedAt = DateTime.Now;
            line.UpdatedBy = _userContext.UserId;
        }
    }
}
