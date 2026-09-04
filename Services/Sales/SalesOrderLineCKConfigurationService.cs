using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderLineCKConfigurationService
        : ISalesOrderLineCKConfigurationService
    {
        private readonly AppDbContext _context;

        public SalesOrderLineCKConfigurationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalesOrderLineCKConfiguration>>
            GetBySalesOrderLineIdAsync(long salesOrderLineId)
        {
            return await _context.SalesOrderLineCKConfigurations
                .Where(x => x.SalesOrderLineId == salesOrderLineId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<SalesOrderLineCKConfiguration?>
            GetByIdAsync(long id)
        {
            return await _context.SalesOrderLineCKConfigurations
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<SalesOrderLineCKConfiguration>
            CreateAsync(SalesOrderLineCKConfiguration configuration)
        {
            var lineExists = await _context.SalesOrderLines.AnyAsync(x => x.Id == configuration.SalesOrderLineId);

            if (!lineExists)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            configuration.Id = 0;
            configuration.CreatedAt = DateTime.Now;
            _context.SalesOrderLineCKConfigurations.Add(configuration);
            await _context.SaveChangesAsync();
            return configuration;
        }

        public async Task<SalesOrderLineCKConfiguration?>UpdateAsync(long id,SalesOrderLineCKConfiguration configuration)
        {
            var existing = await _context.SalesOrderLineCKConfigurations.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null) { return null; }

            existing.ProductTypeId = configuration.ProductTypeId;
            existing.ProductId = configuration.ProductId;
            existing.CatalogCode = configuration.CatalogCode;

            existing.FootRal = configuration.FootRal;
            existing.BodyWingRal = configuration.BodyWingRal;

            existing.PlasticColor1No = configuration.PlasticColor1No;
            existing.PlasticColor2No = configuration.PlasticColor2No;

            existing.MainAssemblyCode = configuration.MainAssemblyCode;
            existing.ManualAssemblyCode = configuration.ManualAssemblyCode;

            existing.MainAssemblyCodeExists =configuration.MainAssemblyCodeExists;
            existing.ManualAssemblyCodeExists =configuration.ManualAssemblyCodeExists;
            existing.ProductGroupId =configuration.ProductGroupId;

            existing.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.SalesOrderLineCKConfigurations.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) { return false; }
            _context.SalesOrderLineCKConfigurations.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
