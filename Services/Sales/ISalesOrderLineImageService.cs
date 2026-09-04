using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderLineImageService
    {
        Task<List<SalesOrderLineImage>> GetBySalesOrderLineIdAsync(long salesOrderLineId);
        Task<SalesOrderLineImage?> GetByIdAsync(long id);
        Task<SalesOrderLineImage> CreateAsync(long salesOrderLineId,int imageTypeId,IFormFile file,int createdBy);
        Task<SalesOrderLineImage?> UpdateAsync(long id,SalesOrderLineImage image);
        Task<bool> DeleteAsync(long id);
    }
}