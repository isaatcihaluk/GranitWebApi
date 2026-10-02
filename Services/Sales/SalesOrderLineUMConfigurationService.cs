using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderLineUMConfigurationService : ISalesOrderLineUMConfigurationService
    {
        private readonly AppDbContext _context;

        public SalesOrderLineUMConfigurationService(AppDbContext context)
        {
            _context = context;
        }

        // GET BY SALES ORDER LINE ID
        public async Task<List<SalesOrderLineUMConfiguration>> GetBySalesOrderLineIdAsync(long salesOrderLineId)
        {
            return await _context.SalesOrderLineUMConfigurations
                .Where(x => x.SalesOrderLineId == salesOrderLineId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        // GET BY ID
        public async Task<SalesOrderLineUMConfiguration?> GetByIdAsync(long id)
        {
            return await _context.SalesOrderLineUMConfigurations.FirstOrDefaultAsync(x => x.Id == id);
        }

        // CREATE
        public async Task<SalesOrderLineUMConfiguration> CreateAsync(SalesOrderLineUMConfiguration configuration)
        {
            var lineExists =await _context.SalesOrderLines.AnyAsync(x => x.Id == configuration.SalesOrderLineId);

            if (!lineExists)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            configuration.Id = 0;
            configuration.CreatedAt = DateTime.Now;
            _context.SalesOrderLineUMConfigurations.Add(configuration);
            await _context.SaveChangesAsync();
            return configuration;
        }

        // UPDATE

        public async Task<SalesOrderLineUMConfiguration?> UpdateAsync(long id,SalesOrderLineUMConfiguration configuration)
        {
            var existing = await _context.SalesOrderLineUMConfigurations.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return null;
            }

            existing.BodyType =configuration.BodyType;
            existing.BodyCode =configuration.BodyCode;
            existing.IroningBoardCode =configuration.IroningBoardCode;
            existing.FootCode =configuration.FootCode;

            existing.BodyIroningRal =configuration.BodyIroningRal;
            existing.BodyIroningColor =configuration.BodyIroningColor;

            existing.FootRal =configuration.FootRal;
            existing.FootColor =configuration.FootColor;

            existing.FabricCode =configuration.FabricCode;
            existing.FabricName =configuration.FabricName;

            existing.SpongeCode =configuration.SpongeCode;
            existing.SpongeName =configuration.SpongeName;
            existing.SpongeQuantity =configuration.SpongeQuantity;

            existing.HasFis =configuration.HasFis;
            existing.FisType =configuration.HasFis? configuration.FisType: null;
            existing.FisCode =configuration.HasFis? configuration.FisCode: null;
            existing.FisName =configuration.HasFis? configuration.FisName: null;

            existing.PlasticCombinationNo =configuration.PlasticCombinationNo;
            existing.PlasticCombinationDescription =configuration.PlasticCombinationDescription;

            existing.MainAssemblyCode =configuration.MainAssemblyCode;
            existing.ManualAssemblyCode =configuration.ManualAssemblyCode;
            existing.MainAssemblyCodeExists =configuration.MainAssemblyCodeExists;
            existing.ManualAssemblyCodeExists =configuration.ManualAssemblyCodeExists;

            existing.ProductGroupId =configuration.ProductGroupId;
            existing.UpdatedAt =DateTime.Now;
            existing.UpdatedBy =configuration.UpdatedBy;
            await _context.SaveChangesAsync();
            return existing;
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            var existing =await _context.SalesOrderLineUMConfigurations.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return false;
            }
            _context.SalesOrderLineUMConfigurations.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}