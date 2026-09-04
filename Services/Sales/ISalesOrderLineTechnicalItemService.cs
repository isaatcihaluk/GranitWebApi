using GranitWebApi.Models.NETSISMODELLER;
using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderLineTechnicalItemService
    {
        Task<List<SalesOrderLineTechnicalItem>> GetBySalesOrderLineIdAsync(long salesOrderLineId);
        Task<List<SalesOrderLineTechnicalItem>> GetByImageIdAsync(long imageId);
        Task<SalesOrderLineTechnicalItem?> GetByIdAsync(long id);
        Task<SalesOrderLineTechnicalItem> CreateAsync(SalesOrderLineTechnicalItem technicalItem);
        Task<SalesOrderLineTechnicalItem?> UpdateAsync(long id, SalesOrderLineTechnicalItem technicalItem);
        Task<bool> DeleteAsync(long id);
        Task<List<TBLSTSABIT>> GetStockInfoAsync();
        Task SaveTechnicalItemsAsync(long salesOrderLineId,List<SalesOrderLineTechnicalItem> technicalItems);
    }
}
