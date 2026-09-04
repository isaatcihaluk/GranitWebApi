using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderLineImageService : ISalesOrderLineImageService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public SalesOrderLineImageService(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================================================
        // LİSTELE
        // =========================================================

        public async Task<List<SalesOrderLineImage>> GetBySalesOrderLineIdAsync(
            long salesOrderLineId)
        {
            return await _context.SalesOrderLineImages
                .Include(x => x.ImageType)
                .Where(x =>
                    x.SalesOrderLineId == salesOrderLineId &&
                    x.IsActive)
                .OrderBy(x => x.ImageType!.DisplayOrder)
                .ToListAsync();
        }

        // =========================================================
        // ID İLE GETİR
        // =========================================================

        public async Task<SalesOrderLineImage?> GetByIdAsync(long id)
        {
            return await _context.SalesOrderLineImages
                .Include(x => x.ImageType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // =========================================================
        // DOSYA OLUŞTUR
        // =========================================================

        public async Task<SalesOrderLineImage> CreateAsync(
            long salesOrderLineId,
            int imageTypeId,
            IFormFile file,
            int createdBy)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Dosya seçilmedi.");

            // =====================================================
            // SİPARİŞ SATIRI
            // =====================================================

            var line = await _context.SalesOrderLines
                .FirstOrDefaultAsync(x => x.Id == salesOrderLineId);

            if (line == null)
                throw new Exception(
                    "Sipariş satırı bulunamadı.");

            // =====================================================
            // GÖRSEL TİPİ
            // =====================================================

            var imageType = await _context.SalesOrderImageTypes
                .FirstOrDefaultAsync(x =>
                    x.Id == imageTypeId &&
                    x.IsActive);

            if (imageType == null)
                throw new Exception(
                    "Görsel tipi bulunamadı veya pasif durumda.");

            // =====================================================
            // AYNI TİP ZATEN VAR MI?
            // =====================================================

            var existingImage = await _context.SalesOrderLineImages
                .FirstOrDefaultAsync(x =>
                    x.SalesOrderLineId == salesOrderLineId &&
                    x.ImageTypeId == imageTypeId &&
                    x.IsActive);

            if (existingImage != null)
            {
                throw new Exception(
                    $"Bu sipariş kalemi için '{imageType.Name}' görseli zaten mevcut.");
            }

            // =====================================================
            // ROOT PATH
            // =====================================================

            var rootPath = _configuration["FileStorage:RootPath"];

            if (string.IsNullOrWhiteSpace(rootPath))
                throw new Exception(
                    "FileStorage:RootPath ayarı bulunamadı.");

            // =====================================================
            // KLASÖR
            // =====================================================

            var directoryPath = Path.Combine(
                rootPath,
                "SalesOrders",
                line.SalesOrderId.ToString(),
                salesOrderLineId.ToString(),
                imageType.Code
            );

            Directory.CreateDirectory(directoryPath);

            // =====================================================
            // DOSYA UZANTISI
            // =====================================================

            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".bin";

            // =====================================================
            // BENZERSİZ DOSYA ADI
            // =====================================================

            var uniqueFileName =
                $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            var physicalFilePath = Path.Combine(
                directoryPath,
                uniqueFileName
            );

            // =====================================================
            // FİZİKSEL DOSYA
            // =====================================================

            try
            {
                await using (var stream = new FileStream(
                    physicalFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                // =================================================
                // DB'YE GÖRECELİ PATH
                // =================================================

                var relativePath = Path.Combine(
                    "SalesOrders",
                    line.SalesOrderId.ToString(),
                    salesOrderLineId.ToString(),
                    imageType.Code,
                    uniqueFileName
                );

                relativePath = relativePath.Replace(
                    Path.DirectorySeparatorChar,
                    '/'
                );

                // =================================================
                // VERSION
                // =================================================

                var lastVersion =
                    await _context.SalesOrderLineImages
                        .Where(x =>
                            x.SalesOrderLineId == salesOrderLineId &&
                            x.ImageTypeId == imageTypeId)
                        .Select(x => (int?)x.VersionNo)
                        .MaxAsync() ?? 0;

                // =================================================
                // DB KAYDI
                // =================================================

                var image = new SalesOrderLineImage
                {
                    SalesOrderLineId = salesOrderLineId,
                    ImageTypeId = imageTypeId,

                    FileName = file.FileName,
                    FilePath = relativePath,

                    VersionNo = lastVersion + 1,

                    IsActive = true,

                    CreatedAt = DateTime.Now,
                    CreatedBy = createdBy
                };

                _context.SalesOrderLineImages.Add(image);

                await _context.SaveChangesAsync();

                return image;
            }
            catch
            {
                // DB kaydı başarısız olursa fiziksel dosyayı
                // yetim bırakma.

                if (File.Exists(physicalFilePath))
                {
                    File.Delete(physicalFilePath);
                }

                throw;
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<SalesOrderLineImage?> UpdateAsync(
            long id,
            SalesOrderLineImage image)
        {
            var existing = await _context.SalesOrderLineImages
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
                return null;

            existing.FileName = image.FileName;
            existing.FilePath = image.FilePath;

            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = image.UpdatedBy;

            await _context.SaveChangesAsync();

            return existing;
        }

        // =========================================================
        // DELETE
        // =========================================================

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.SalesOrderLineImages
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
                return false;

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}