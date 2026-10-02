using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderLineUMConfigurationService
    {
        Task<List<SalesOrderLineUMConfiguration>> GetBySalesOrderLineIdAsync(long salesOrderLineId);
        Task<SalesOrderLineUMConfiguration?> GetByIdAsync(long id);
        Task<SalesOrderLineUMConfiguration> CreateAsync(SalesOrderLineUMConfiguration configuration);
        Task<SalesOrderLineUMConfiguration?> UpdateAsync(long id, SalesOrderLineUMConfiguration configuration);
        Task<bool> DeleteAsync(long id);
    }
}