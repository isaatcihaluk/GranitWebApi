using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using GranitWebApi.Services.Sales;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderLineService : ISalesOrderLineService
    {
        private readonly AppDbContext _context;
        private readonly ErpDbContext _erpContext;
        private readonly UserContext _userContext;
        private readonly IConfiguration _configuration;

        public SalesOrderLineService(AppDbContext context, ErpDbContext erpContext, UserContext userContext, IConfiguration configuration)
        {
            _context = context;
            _erpContext = erpContext;
            _userContext = userContext;
            _configuration = configuration;
        }

        // GET BY SALES ORDER
        public async Task<List<SalesOrderLine>> GetBySalesOrderIdAsync(long salesOrderId)
        {
            return await _context.SalesOrderLines
                .Where(x =>
                    x.SalesOrderId == salesOrderId &&
                    !x.DeletedFlag)
                .OrderBy(x => x.LineNumber)
                .ToListAsync();
        }

        // GET BY ID
        public async Task<SalesOrderLine?> GetByIdAsync(long id)
        {
            return await _context.SalesOrderLines
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedFlag);
        }

        // GET IMAGE
        public async Task<SalesOrderLineImage?> GetImageAsync(long lineId, long imageId)
        {
            return await _context.SalesOrderLineImages
                .FirstOrDefaultAsync(x =>
                    x.Id == imageId &&
                    x.SalesOrderLineId == lineId &&
                    x.IsActive);
        }

        // CREATE
        public async Task<SalesOrderLine> CreateAsync(SalesOrderLine line)
        {

            var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == line.SalesOrderId);
            if (salesOrder == null) { throw new Exception($"SalesOrder bulunamadı. Id: {line.SalesOrderId}"); }

            var lineExists =
                await _context.SalesOrderLines
                    .AnyAsync(x =>
                        x.SalesOrderId == line.SalesOrderId &&
                        x.LineNumber == line.LineNumber &&
                        !x.DeletedFlag);

            if (lineExists) { throw new Exception($"Bu siparişte {line.LineNumber} numaralı satır zaten mevcut."); }
            if (line.Quantity <= 0) { throw new Exception("Ürün miktarı 0'dan büyük olmalıdır."); }
            if (string.IsNullOrWhiteSpace(line.ProductGroupId)) { throw new Exception("Ürün grubu belirtilmelidir."); }

            line.Id = 0;
            line.MainAssemblyCode = null;
            line.ManualAssemblyCode = null;
            line.MainAssemblyControl = false;
            line.Status = "TASLAK";
            line.CreatedAt = DateTime.Now;
            line.CreatedBy = _userContext.UserId;
            line.UpdatedAt = null;
            line.UpdatedBy = null;
            line.DeletedFlag = false;
            _context.SalesOrderLines.Add(line);
            salesOrder.Status = "TASLAK";
            await _context.SaveChangesAsync();
            return line;
        }

        // UPDATE
        public async Task<SalesOrderLine?> UpdateAsync(long id, SalesOrderLine line)
        {
            var existingLine = await _context.SalesOrderLines.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedFlag);
            var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == existingLine.SalesOrderId);
            if (salesOrder == null) { throw new Exception($"SalesOrder bulunamadı. Id: {existingLine.SalesOrderId}"); }

            if (existingLine == null) { return null; }
            if (line.Quantity <= 0) { throw new Exception("Ürün miktarı 0'dan büyük olmalıdır."); }
            if (string.IsNullOrWhiteSpace(line.ProductGroupId)) { throw new Exception("Ürün grubu belirtilmelidir."); }

            var lineExists =
                await _context.SalesOrderLines
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.SalesOrderId == existingLine.SalesOrderId &&
                        x.LineNumber == line.LineNumber &&
                        !x.DeletedFlag);

            if (lineExists) { throw new Exception($"Bu siparişte {line.LineNumber} numaralı satır zaten mevcut."); }

            existingLine.LineNumber = line.LineNumber;
            existingLine.ProductGroupId = line.ProductGroupId;
            existingLine.Quantity = line.Quantity;
            existingLine.ProductName = line.ProductName;
            existingLine.CatalogCode = line.CatalogCode;
            existingLine.UpdatedAt = DateTime.Now;
            existingLine.UpdatedBy = _userContext.UserId;
            salesOrder.Status = "TASLAK";
            await _context.SaveChangesAsync();
            return existingLine;
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(long lineId)
        {
            var line =
                await _context.SalesOrderLines
                    .FirstOrDefaultAsync(x =>
                        x.Id == lineId &&
                        !x.DeletedFlag);

            if (line == null) { return false; }
            var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == line.SalesOrderId);
            if (salesOrder == null) { throw new Exception($"SalesOrder bulunamadı. Id: {line.SalesOrderId}"); }

            line.DeletedFlag = true;
            line.UpdatedAt = DateTime.Now;
            line.UpdatedBy = _userContext.UserId;
            salesOrder.Status = "TASLAK";
            await _context.SaveChangesAsync();
            return true;
        }

        // CREATE COMPLETE
        public async Task<SalesOrderLine?> CreateCompleteAsync(SalesOrderLine line, SalesOrderLineCKConfiguration? ckConfiguration,
            SalesOrderLineUMConfiguration? umConfiguration, List<(int ImageTypeId, IFormFile File)> files)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var savedFiles = new List<string>();

            try
            {
                var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == line.SalesOrderId);
                if (salesOrder == null) { throw new Exception($"SalesOrder bulunamadı. Id: {line.SalesOrderId}"); }

                var lineNumber =
                    await _context.SalesOrderLines
                        .CountAsync(x =>
                            x.SalesOrderId == line.SalesOrderId);

                line.LineNumber = lineNumber;

                if (line.Quantity <= 0) { throw new Exception("Ürün miktarı 0'dan büyük olmalıdır."); }
                if (string.IsNullOrWhiteSpace(line.ProductGroupId)) { throw new Exception("Ürün grubu belirtilmelidir."); }

                if (line.ProductGroupId == "1")
                {
                    // =====================================================
                    // CK VALIDATION
                    // =====================================================

                    if (ckConfiguration == null)
                    {
                        throw new Exception("CK ürün konfigürasyonu belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.ProductId))
                    {
                        throw new Exception("Ürün belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.CatalogCode))
                    {
                        throw new Exception("Katalog kodu belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.FootRal))
                    {
                        throw new Exception("Ayak rengi belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.BodyWingRal))
                    {
                        throw new Exception("Gövde / Kanat rengi belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.PlasticColor1No))
                    {
                        throw new Exception("Plastik rengi belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(ckConfiguration.PlasticColor2No))
                    {
                        throw new Exception("Plastik rengi 2 belirtilmelidir.");
                    }
                }
                else if (line.ProductGroupId == "2")
                {
                    // UM VALIDATION
                    if (umConfiguration == null)
                    {
                        throw new Exception("UM ürün konfigürasyonu belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.BodyType))
                    {
                        throw new Exception("Gövde tipi belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.BodyCode))
                    {
                        throw new Exception("Gövde belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.IroningBoardCode))
                    {
                        throw new Exception("Ütülük belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.FootCode))
                    {
                        throw new Exception("Ayak belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.BodyIroningRal))
                    {
                        throw new Exception("Gövde / Ütülük rengi belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.BodyIroningColor))
                    {
                        throw new Exception("Gövde / Ütülük rengi belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.FootRal))
                    {
                        throw new Exception("Ayak rengi belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.FootColor))
                    {
                        throw new Exception("Ayak rengi belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.FabricCode))
                    {
                        throw new Exception("Kumaş belirtilmelidir.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.SpongeCode))
                    {
                        throw new Exception("Sünger belirtilmelidir.");
                    }
                    if (umConfiguration.SpongeQuantity <= 0)
                    {
                        throw new Exception("Sünger miktarı 0'dan büyük olmalıdır.");
                    }
                    if (string.IsNullOrWhiteSpace(umConfiguration.PlasticCombinationNo))
                    {
                        throw new Exception("Plastik rengi belirtilmelidir.");
                    }
                }
                else if (line.ProductGroupId == "5")
                {
                    if (string.IsNullOrWhiteSpace(line.CatalogCode)) { throw new Exception("Yedek kılıf türü belirtilmelidir."); }
                    if (string.IsNullOrWhiteSpace(line.YKFabricCode)) { throw new Exception("Yedek kılıf kumaşı belirtilmelidir."); }
                }
                else
                {
                    throw new Exception(
                        $"Bu ürün grubu için kayıt işlemi desteklenmiyor. " +
                        $"ProductGroupId: '{line.ProductGroupId}'");
                }
                line.Id = 0;
                line.MainAssemblyCode = null;
                line.ManualAssemblyCode = null;
                line.MainAssemblyControl = null;
                line.Status = "TASLAK";
                line.CreatedAt = DateTime.Now;
                line.CreatedBy = _userContext.UserId;
                line.UpdatedAt = null;
                line.UpdatedBy = null;
                line.DeletedFlag = false;
                _context.SalesOrderLines.Add(line);
                salesOrder.Status = "TASLAK";
                await _context.SaveChangesAsync();

                if (line.ProductGroupId == "1")
                {
                    // CK CONFIGURATION KAYDI
                    ckConfiguration!.Id = 0;
                    ckConfiguration.SalesOrderLineId = line.Id;
                    ckConfiguration.ProductGroupId = line.ProductGroupId;
                    ckConfiguration.CreatedAt = DateTime.Now;
                    ckConfiguration.CreatedBy = _userContext.UserId;
                    ckConfiguration.UpdatedAt = null;
                    ckConfiguration.UpdatedBy = null;

                    _context.SalesOrderLineCKConfigurations.Add(ckConfiguration);
                }
                else if (line.ProductGroupId == "2")
                {
                    // UM CONFIGURATION KAYDI

                    umConfiguration!.Id = 0;
                    umConfiguration.SalesOrderLineId = line.Id;
                    umConfiguration.ProductGroupId = line.ProductGroupId;
                    umConfiguration.CreatedAt = DateTime.Now;
                    umConfiguration.CreatedBy = _userContext.UserId;
                    umConfiguration.UpdatedAt = null;
                    umConfiguration.UpdatedBy = null;

                    _context.SalesOrderLineUMConfigurations.Add(umConfiguration);
                }
                else if (line.ProductGroupId == "5")
                {
                    if (string.IsNullOrWhiteSpace(line.CatalogCode)) { throw new Exception("Yedek kılıf türü belirtilmelidir."); }
                    if (string.IsNullOrWhiteSpace(line.YKFabricCode)) { throw new Exception("Yedek kılıf kumaşı belirtilmelidir."); }
                }

                await _context.SaveChangesAsync();
                var rootPath = _configuration["FileStorage:RootPath"];

                if (string.IsNullOrWhiteSpace(rootPath)) { throw new Exception("FileStorage:RootPath tanımlı değil."); }
                if (!Directory.Exists(rootPath)) { throw new Exception($"Root klasörü bulunamadı veya erişilemiyor: '{rootPath}'"); }
                var salesOrderFolder = Path.Combine(rootPath, "SalesOrders", salesOrder.Id.ToString(), line.Id.ToString());
                try
                {
                    Directory.CreateDirectory(salesOrderFolder);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Dosya klasörü oluşturulamadı. " + $"Klasör: {salesOrderFolder}", ex);
                }

                foreach (var item in files)
                {
                    var imageType = await _context.SalesOrderImageTypes.FirstOrDefaultAsync(x => x.Id == item.ImageTypeId && x.IsActive);
                    if (imageType == null) { throw new Exception($"Görsel tipi bulunamadı. " + $"Id: {item.ImageTypeId}"); }

                    var duplicateType = files.Count(x => x.ImageTypeId == item.ImageTypeId);
                    if (duplicateType > 1) { throw new Exception($"{imageType.Name} için yalnızca bir dosya yüklenebilir."); }
                    if (item.File == null || item.File.Length == 0) { throw new Exception($"{imageType.Name} için dosya boş."); }

                    var lastVersion =
                        await _context.SalesOrderLineImages
                            .Where(x =>
                                x.SalesOrderLineId == line.Id &&
                                x.ImageTypeId == item.ImageTypeId)
                            .Select(x => (int?)x.VersionNo)
                            .MaxAsync() ?? 0;

                    var versionNo = lastVersion + 1;

                    var typeFolder = Path.Combine(salesOrderFolder, imageType.Code);
                    Directory.CreateDirectory(typeFolder);

                    var originalFileName = Path.GetFileName(item.File.FileName);
                    var extension = Path.GetExtension(originalFileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    {
                        fileNameWithoutExtension = fileNameWithoutExtension.Replace(invalidChar, '_');
                    }
                    var storedFileName = $"v{versionNo}_{fileNameWithoutExtension}{extension}";
                    var physicalFilePath = Path.Combine(typeFolder, storedFileName);

                    await using (var stream = new FileStream(physicalFilePath, FileMode.CreateNew))
                    {
                        await item.File.CopyToAsync(stream);
                    }
                    savedFiles.Add(physicalFilePath);

                    var relativePath = Path.Combine("SalesOrders", salesOrder.Id.ToString(), line.Id.ToString(), imageType.Code, storedFileName);

                    var image = new SalesOrderLineImage
                    {
                        SalesOrderLineId = line.Id,
                        ImageTypeId = imageType.Id,
                        FileName = originalFileName,
                        FilePath = relativePath,
                        VersionNo = versionNo,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = _userContext.UserId
                    };

                    _context.SalesOrderLineImages.Add(image);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                line.SalesOrder = null;
                return line;
            }
            catch
            {
                await transaction.RollbackAsync();
                foreach (var filePath in savedFiles)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch
                    {
                        // Cleanup hatası ana hatayı ezmesin.
                    }
                }
                throw;
            }
        }

        // GET FOR EDIT
        public async Task<SalesOrderLineEditResult?> GetForEditAsync(long lineId)
        {
            var line = await _context.SalesOrderLines
                .Include(x => x.CKConfiguration)
                .Include(x => x.UMConfiguration)
                .Include(x => x.Images)
                .ThenInclude(x => x.ImageType)
                .FirstOrDefaultAsync(x =>
                    x.Id == lineId &&
                    !x.DeletedFlag);

            if (line == null)
            {
                return null;
            }

            var result = new SalesOrderLineEditResult
            {
                LineId = line.Id,
                SalesOrderId = line.SalesOrderId,
                LineNumber = line.LineNumber,
                ProductGroupId = line.ProductGroupId,
                Quantity = line.Quantity,
                ProductName = line.ProductName,
                CatalogCode = line.CatalogCode,
                YKFabricCode = line.YKFabricCode,
                KoliId = line.KoliId,
                KoliKod = line.KoliKod,
                KoliIciMiktar = line.KoliIciMiktar,
                Images = line.Images?.Where(x => x.IsActive)
                    .OrderBy(x => x.ImageTypeId).ThenByDescending(x => x.VersionNo)
                    .Select(x =>
                        new SalesOrderLineEditImage
                        {
                            Id = x.Id,
                            ImageTypeId = x.ImageTypeId,
                            FileName = x.FileName,
                            FilePath = x.FilePath,
                            VersionNo = x.VersionNo
                        })
                    .ToList() ?? new List<SalesOrderLineEditImage>()
            };

            // CK
            if (line.ProductGroupId == "1")
            {
                var configuration = line.CKConfiguration;
                if (configuration == null)
                {
                    throw new Exception($"SalesOrderLineId={lineId} için CK configuration bulunamadı.");
                }

                result.ProductTypeId = configuration.ProductTypeId;
                result.ProductId = configuration.ProductId;
                result.ConfigurationCatalogCode = configuration.CatalogCode;
                result.FootRal = configuration.FootRal;
                result.BodyWingRal = configuration.BodyWingRal;
                result.PlasticColor1No = configuration.PlasticColor1No;
                result.PlasticColor2No = configuration.PlasticColor2No;
                result.MainAssemblyCode = configuration.MainAssemblyCode;
                result.ManualAssemblyCode = configuration.ManualAssemblyCode;
                result.MainAssemblyCodeExists = configuration.MainAssemblyCodeExists;
                result.ManualAssemblyCodeExists = configuration.ManualAssemblyCodeExists;
            }
            // UM
            else if (line.ProductGroupId == "2")
            {
                var configuration = line.UMConfiguration;
                if (configuration == null)
                {
                    throw new Exception($"SalesOrderLineId={lineId} için UM configuration bulunamadı.");
                }

                result.BodyType = configuration.BodyType;
                result.BodyCode = configuration.BodyCode;
                result.IroningBoardCode = configuration.IroningBoardCode;
                result.FootCode = configuration.FootCode;
                result.BodyIroningRal = configuration.BodyIroningRal;
                result.BodyIroningColor = configuration.BodyIroningColor;
                result.FootRal = configuration.FootRal;
                result.FootColor = configuration.FootColor;
                result.FabricCode = configuration.FabricCode;
                result.FabricName = configuration.FabricName;
                result.SpongeCode = configuration.SpongeCode;
                result.SpongeName = configuration.SpongeName;
                result.SpongeQuantity = configuration.SpongeQuantity;
                result.HasFis = configuration.HasFis;
                result.FisType = configuration.FisType;
                result.FisCode = configuration.FisCode;
                result.FisName = configuration.FisName;
                result.PlasticCombinationNo = configuration.PlasticCombinationNo;
                result.PlasticCombinationDescription = configuration.PlasticCombinationDescription;
                result.MainAssemblyCode = configuration.MainAssemblyCode;
                result.ManualAssemblyCode = configuration.ManualAssemblyCode;
                result.MainAssemblyCodeExists = configuration.MainAssemblyCodeExists;
                result.ManualAssemblyCodeExists = configuration.ManualAssemblyCodeExists;
            }
            // YK
            else if (line.ProductGroupId == "5") 
            { 
                result.YKFabricCode = line.YKFabricCode; 
            } 
            else
            {
                throw new Exception($"Desteklenmeyen ürün grubu: {line.ProductGroupId}");
            }

            return result;
        }

        // UPDATE COMPLETE
        public async Task<SalesOrderLine?> UpdateCompleteAsync(long id, SalesOrderLineUpdateModel request,
            List<(int ImageTypeId, IFormFile File)> files, List<long> existingImageIds)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var savedFiles = new List<string>();
            try
            {
                if (request.LineId != id)
                {
                    throw new Exception("Güncellenecek sipariş satırı bilgisi uyuşmuyor.");
                }

                var line = await _context.SalesOrderLines.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedFlag);
                if (line == null) { return null; }

                var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == line.SalesOrderId);
                if (salesOrder == null) { throw new Exception($"SalesOrder bulunamadı. Id: {line.SalesOrderId}"); }

                if (request.SalesOrderId != line.SalesOrderId) { throw new Exception("Sipariş satırı farklı bir siparişe taşınamaz."); }
                if (files == null) { files = new List<(int ImageTypeId, IFormFile File)>(); }
                if (existingImageIds == null) { existingImageIds = new List<long>(); }
                if (request.Quantity <= 0) { throw new Exception("Ürün miktarı 0'dan büyük olmalıdır."); }
                if (string.IsNullOrWhiteSpace(request.ProductGroupId)) { throw new Exception("Ürün grubu belirtilmelidir."); }

                if (request.ProductGroupId == "1")
                {
                    // CK validation
                    if (string.IsNullOrWhiteSpace(request.ProductId))
                        throw new Exception("Ürün belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.ConfigurationCatalogCode))
                        throw new Exception("Katalog kodu belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FootRal))
                        throw new Exception("Ayak rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.BodyWingRal))
                        throw new Exception("Gövde / Kanat rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.PlasticColor1No))
                        throw new Exception("Plastik rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.PlasticColor2No))
                        throw new Exception("Plastik rengi 2 belirtilmelidir.");
                }
                else if (request.ProductGroupId == "2")
                {
                    // UM validation
                    if (string.IsNullOrWhiteSpace(request.BodyType))
                        throw new Exception("UM gövde tipi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.BodyCode))
                        throw new Exception("UM gövde belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.IroningBoardCode))
                        throw new Exception("UM ütülük belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FootCode))
                        throw new Exception("UM ayak belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.BodyIroningRal))
                        throw new Exception("UM gövde / ütülük rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.BodyIroningColor))
                        throw new Exception("UM gövde / ütülük renk adı belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FootRal))
                        throw new Exception("UM ayak RAL rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FootColor))
                        throw new Exception("UM ayak rengi belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FabricCode))
                        throw new Exception("UM kumaş belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.FabricName))
                        throw new Exception("UM kumaş adı belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.SpongeCode))
                        throw new Exception("UM sünger belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.SpongeName))
                        throw new Exception("UM sünger adı belirtilmelidir.");

                    if (request.SpongeQuantity == null || request.SpongeQuantity <= 0)
                        throw new Exception("UM sünger miktarı 0'dan büyük olmalıdır.");

                    if (request.HasFis)
                    {
                        if (string.IsNullOrWhiteSpace(request.FisType))
                            throw new Exception("UM fiş tipi belirtilmelidir.");

                        if (string.IsNullOrWhiteSpace(request.FisCode))
                            throw new Exception("UM fiş kodu belirtilmelidir.");

                        if (string.IsNullOrWhiteSpace(request.FisName))
                            throw new Exception("UM fiş adı belirtilmelidir.");
                    }

                    if (string.IsNullOrWhiteSpace(request.PlasticCombinationNo))
                        throw new Exception("UM plastik kombinasyonu belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.PlasticCombinationDescription))
                        throw new Exception("UM plastik kombinasyon açıklaması belirtilmelidir.");
                }
                else if (line.ProductGroupId == "5")
                {
                    if (string.IsNullOrWhiteSpace(request.CatalogCode))
                        throw new Exception("Yedek kılıf türü belirtilmelidir.");

                    if (string.IsNullOrWhiteSpace(request.YKFabricCode))
                        throw new Exception("Yedek kılıf kumaşı belirtilmelidir.");
                }
                else
                {
                    throw new Exception($"Desteklenmeyen ürün grubu: '{request.ProductGroupId}'");
                }

                var lineExists = await _context.SalesOrderLines
                        .AnyAsync(x => x.Id != id && x.SalesOrderId == line.SalesOrderId &&
                            x.LineNumber == request.LineNumber && !x.DeletedFlag);

                if (lineExists)
                {
                    throw new Exception(
                        $"Bu siparişte {request.LineNumber} numaralı " +
                        $"satır zaten mevcut.");
                }

                line.LineNumber = request.LineNumber;
                line.ProductGroupId = request.ProductGroupId;
                line.Quantity = request.Quantity;
                line.ProductName = request.ProductName;
                line.CatalogCode = request.CatalogCode;
                line.KoliId = request.KoliId;
                line.KoliKod = request.KoliKod;
                line.KoliIciMiktar = request.KoliIciMiktar;
                line.UpdatedAt = DateTime.Now;
                line.UpdatedBy = _userContext.UserId;
                line.YKFabricCode = request.YKFabricCode;
                line.ShrinkliKod = "";
                line.ShrinkliAd = "";
                salesOrder.Status = "TASLAK";

                if (request.ProductGroupId == "1")
                {
                    // CK CONFIGURATION UPDATE

                    var configuration = await _context.SalesOrderLineCKConfigurations.FirstOrDefaultAsync(x => x.SalesOrderLineId == line.Id);

                    if (configuration == null)
                    {
                        throw new Exception(
                            $"Sipariş satırına ait CK configuration bulunamadı. " +
                            $"SalesOrderLineId: {line.Id}");
                    }

                    configuration.ProductTypeId = request.ProductTypeId;
                    configuration.ProductId = request.ProductId;
                    configuration.CatalogCode = request.ConfigurationCatalogCode;
                    configuration.FootRal = request.FootRal;
                    configuration.BodyWingRal = request.BodyWingRal;
                    configuration.PlasticColor1No = request.PlasticColor1No;
                    configuration.PlasticColor2No = request.PlasticColor2No;
                    configuration.ProductGroupId = request.ProductGroupId;
                    configuration.UpdatedAt = DateTime.Now;
                    configuration.UpdatedBy = _userContext.UserId;
                }
                else if (request.ProductGroupId == "2")
                {
                    // UM CONFIGURATION UPDATE
                    var configuration = await _context.SalesOrderLineUMConfigurations
                            .FirstOrDefaultAsync(x => x.SalesOrderLineId == line.Id);

                    if (configuration == null)
                    {
                        throw new Exception(
                            $"Sipariş satırına ait UM configuration bulunamadı. " +
                            $"SalesOrderLineId: {line.Id}");
                    }

                    configuration.BodyType = request.BodyType!;
                    configuration.BodyCode = request.BodyCode!;
                    configuration.IroningBoardCode = request.IroningBoardCode!;
                    configuration.FootCode = request.FootCode!;
                    configuration.BodyIroningRal = request.BodyIroningRal!;
                    configuration.BodyIroningColor = request.BodyIroningColor!;
                    configuration.FootRal = request.FootRal!;
                    configuration.FootColor = request.FootColor!;
                    configuration.FabricCode = request.FabricCode!;
                    configuration.FabricName = request.FabricName!;
                    configuration.SpongeCode = request.SpongeCode!;
                    configuration.SpongeName = request.SpongeName!;
                    configuration.SpongeQuantity = request.SpongeQuantity!.Value;
                    configuration.HasFis = request.HasFis;
                    configuration.FisType = request.HasFis ? request.FisType : null;
                    configuration.FisCode = request.HasFis ? request.FisCode : null;
                    configuration.FisName = request.HasFis ? request.FisName : null;
                    configuration.PlasticCombinationNo = request.PlasticCombinationNo!;
                    configuration.PlasticCombinationDescription = request.PlasticCombinationDescription!;
                    configuration.ProductGroupId = request.ProductGroupId;
                    configuration.UpdatedAt = DateTime.Now;
                    configuration.UpdatedBy = _userContext.UserId;
                }

                await _context.SaveChangesAsync();
                await _context.SaveChangesAsync();

                var existingImages = await _context.SalesOrderLineImages
                        .Where(x => x.SalesOrderLineId == line.Id && x.IsActive).ToListAsync();

                var keepImageIds = existingImageIds.Distinct().ToHashSet();
                var invalidImageIds = keepImageIds.Where(id => existingImages.All(x => x.Id != id)).ToList();

                if (invalidImageIds.Any())
                {
                    throw new Exception(
                        "Gönderilen mevcut görsellerden bazıları " +
                        "bu sipariş satırına ait değil.");
                }

                foreach (var image in existingImages)
                {
                    if (!keepImageIds.Contains(image.Id))
                    {
                        image.IsActive = false;
                        image.UpdatedAt = DateTime.Now;
                        image.UpdatedBy = _userContext.UserId;
                    }
                }
                await _context.SaveChangesAsync();

                var rootPath = _configuration["FileStorage:RootPath"];
                if (string.IsNullOrWhiteSpace(rootPath)) { throw new Exception("FileStorage:RootPath tanımlı değil."); }
                if (!Directory.Exists(rootPath)) { throw new Exception($"Root klasörü bulunamadı veya erişilemiyor: '{rootPath}'"); }

                var salesOrderFolder = Path.Combine(rootPath, "SalesOrders", salesOrder.Id.ToString(), line.Id.ToString());

                try
                {
                    Directory.CreateDirectory(salesOrderFolder);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Dosya klasörü oluşturulamadı. " + $"Klasör: {salesOrderFolder}", ex);
                }

                foreach (var item in files)
                {
                    var imageType = await _context.SalesOrderImageTypes.FirstOrDefaultAsync(x => x.Id == item.ImageTypeId && x.IsActive);

                    if (imageType == null)
                    {
                        throw new Exception($"Görsel tipi bulunamadı. " + $"Id: {item.ImageTypeId}");
                    }

                    var duplicateType = files.Count(x => x.ImageTypeId == item.ImageTypeId);
                    if (duplicateType > 1)
                    {
                        throw new Exception(
                            $"{imageType.Name} için yalnızca bir dosya " +
                            $"yüklenebilir.");
                    }

                    if (item.File == null || item.File.Length == 0) { throw new Exception($"{imageType.Name} için dosya boş."); }
                    var activeImages = await _context.SalesOrderLineImages
                            .Where(x =>
                                x.SalesOrderLineId == line.Id &&
                                x.ImageTypeId == item.ImageTypeId &&
                                x.IsActive)
                            .ToListAsync();

                    foreach (var oldImage in activeImages)
                    {
                        oldImage.IsActive = false;
                        oldImage.UpdatedAt = DateTime.Now;
                        oldImage.UpdatedBy = _userContext.UserId;
                    }
                    await _context.SaveChangesAsync();

                    var lastVersion =
                        await _context.SalesOrderLineImages
                            .Where(x => x.SalesOrderLineId == line.Id && x.ImageTypeId == item.ImageTypeId)
                            .Select(x => (int?)x.VersionNo).MaxAsync() ?? 0;

                    var versionNo = lastVersion + 1;
                    var typeFolder = Path.Combine(salesOrderFolder, imageType.Code);
                    Directory.CreateDirectory(typeFolder);
                    var originalFileName = Path.GetFileName(item.File.FileName);
                    var extension = Path.GetExtension(originalFileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    {
                        fileNameWithoutExtension = fileNameWithoutExtension.Replace(invalidChar, '_');
                    }
                    var storedFileName = $"v{versionNo}_{fileNameWithoutExtension}{extension}";
                    var physicalFilePath = Path.Combine(typeFolder, storedFileName);

                    await using (var stream = new FileStream(physicalFilePath, FileMode.CreateNew))
                    {
                        await item.File.CopyToAsync(stream);
                    }
                    savedFiles.Add(physicalFilePath);

                    var relativePath = Path.Combine("SalesOrders", salesOrder.Id.ToString(), line.Id.ToString(), imageType.Code, storedFileName);
                    var image = new SalesOrderLineImage
                    {
                        SalesOrderLineId = line.Id,
                        ImageTypeId = imageType.Id,
                        FileName = originalFileName,
                        FilePath = relativePath,
                        VersionNo = versionNo,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = _userContext.UserId
                    };

                    _context.SalesOrderLineImages.Add(image);
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                line.SalesOrder = null;
                return line;
            }
            catch
            {
                await transaction.RollbackAsync();
                foreach (var filePath in savedFiles)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch
                    {
                        // Cleanup hatası ana hatayı ezmesin.
                    }
                }
                throw;
            }
        }

        // GET PRODUCT BOXES
        public async Task<List<ProductBox>> GetProductBoxesAsync(string urunGrupId, string katalogKod, string koliTuru, string? musteriKod)
        {
            var query = _context.ProductBoxes.AsNoTracking().Where(x => x.UrunGrupId == urunGrupId && x.KatalogKod == katalogKod);

            if (koliTuru == "Baskısız")
            {
                query = query.Where(x => x.Durum == "Baskısız");
            }
            else if (koliTuru == "Sarmal")
            {
                query = query.Where(x => x.Durum == "Sarmal");
            }
            else if (koliTuru == "Baskılı")
            {
                if (string.IsNullOrWhiteSpace(musteriKod))
                {
                    throw new Exception("Baskılı koli seçimi için müşteri kodu gereklidir.");
                }
                query = query.Where(x => x.Durum == "Baskılı" && x.MusteriKod == musteriKod);
            }
            else
            {
                throw new Exception("Geçersiz koli türü. " + "Baskısız, Baskılı veya Sarmal olmalıdır.");
            }
            return await query.OrderBy(x => x.KoliIciMiktar).ThenBy(x => x.KoliKod).ToListAsync();
        }
    }
}