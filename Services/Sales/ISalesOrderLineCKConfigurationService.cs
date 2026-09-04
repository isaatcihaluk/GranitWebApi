
using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderLineCKConfigurationService
    {
        Task<List<SalesOrderLineCKConfiguration>> GetBySalesOrderLineIdAsync(long salesOrderLineId);
        Task<SalesOrderLineCKConfiguration?> GetByIdAsync(long id);
        Task<SalesOrderLineCKConfiguration> CreateAsync(SalesOrderLineCKConfiguration configuration);
        Task<SalesOrderLineCKConfiguration?> UpdateAsync(long id,SalesOrderLineCKConfiguration configuration);
        Task<bool> DeleteAsync(long id);
    }
}
