using GranitWebApi.Data;
using GranitWebApi.Models.NETSISMODELLER;
using GranitWebApi.Models.Sales.Definitions;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales.Definitions
{
    public class BUProductDefinitionService
    {
        private readonly AppDbContext _context;
        private readonly ErpDbContext _erpcontext;
        public BUProductDefinitionService(AppDbContext context, ErpDbContext erpcontext)
        {
            _context = context;
            _erpcontext = erpcontext;
        }

        // Katalog kodları
        public async Task<List<BUProductDetail>> GetProductsAsync()
        {
            return await _context.BUProductDetails.AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.KatalogKod))
                .OrderBy(x => x.KatalogKod).ToListAsync();
        }

        // Katalog koduna göre renk + plastik renk
        public async Task<List<BUProductDetailColor>> GetColorsAsync(string katalogKod)
        {
            return await _context.BUProductDetailColors.AsNoTracking()
                .Where(x => x.KatalogKod == katalogKod).OrderBy(x => x.RenkRal).ToListAsync();
        }

        // Etiket Akmaz
        public async Task<List<BUStockDefinition>> GetEtiketAkmazAsync()
        {
            return await _erpcontext.Set<TBLSTSABIT>().AsNoTracking()
                .Where(x =>
                    x.STOK_KODU != null &&
                    x.STOK_KODU.StartsWith("3203.04"))
                .Select(x => new BUStockDefinition
                {
                    Kod = x.STOK_KODU,
                    Ad = x.STOK_ADI
                }).OrderBy(x => x.Kod).ToListAsync();
        }

        // Katalog ürün adına göre kumaş
        public async Task<List<BUStockDefinition>> GetKumasAsync(string katalogKod)
        {
            var urunAdi =
                await _context.BUProductDetails.AsNoTracking().Where(x => x.KatalogKod == katalogKod)
                    .Select(x => x.UrunAdi).FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(urunAdi))
            {
                return new List<BUStockDefinition>();
            }

            var query =
                _erpcontext.Set<TBLSTSABIT>()
                    .AsNoTracking()
                    .Where(x =>
                        x.STOK_KODU != null &&
                        x.STOK_KODU.StartsWith("3211.") &&
                        x.STOK_ADI != null);

            if (urunAdi.Contains("LUX",StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.STOK_ADI.Contains("LUX"));
            }
            else if (urunAdi.Contains("MINIK",StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.STOK_ADI.Contains("MINIK"));
            }
            else if (urunAdi.Contains("DESENLI",StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.STOK_ADI.Contains("DESENLI") && !x.STOK_ADI.Contains("MINIK"));
            }
            else
            {
                query = query.Where(x => x.STOK_ADI.Contains("DUZ"));
            }

            return await query
                .Select(x => new BUStockDefinition
                {
                    Kod = x.STOK_KODU,
                    Ad = x.STOK_ADI
                })
                .OrderBy(x => x.Kod)
                .ToListAsync();
        }
    }
}