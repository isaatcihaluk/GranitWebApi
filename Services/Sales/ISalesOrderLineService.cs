using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using Microsoft.AspNetCore.Http;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderLineService
    {
        Task<List<SalesOrderLine>> GetBySalesOrderIdAsync(long salesOrderId);
        Task<SalesOrderLine?> GetByIdAsync(long id);
        Task<SalesOrderLine> CreateAsync(SalesOrderLine line);
        Task<SalesOrderLine?> CreateCompleteAsync(SalesOrderLine line,SalesOrderLineCKConfiguration configuration,
            List<(int ImageTypeId, IFormFile File)> files);
        Task<SalesOrderLine?> UpdateAsync(long id,SalesOrderLine line);
        Task<SalesOrderLineEditResult?> GetForEditAsync(long lineId);
        Task<SalesOrderLineImage?> GetImageAsync(long lineId,long imageId);
        Task<bool> DeleteAsync(long id);
        Task<SalesOrderLine?> UpdateCompleteAsync(long id,SalesOrderLineUpdateModel request,
            List<(int ImageTypeId, IFormFile File)> files,
            List<long> existingImageIds);
        Task<List<ProductBox>> GetProductBoxesAsync(string urunGrupId,string katalogKod,string koliTuru,string? musteriKod);
    }
}
