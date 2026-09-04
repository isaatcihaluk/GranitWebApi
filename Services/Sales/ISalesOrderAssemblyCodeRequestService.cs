using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderAssemblyCodeRequestService
    {
        Task<List<SalesOrderAssemblyCodeRequest>>GetBySalesOrderLineIdAsync(long salesOrderLineId);
        Task<SalesOrderAssemblyCodeRequest?>GetByIdAsync(long id);
        Task<SalesOrderAssemblyCodeRequest>CreateAsync(SalesOrderAssemblyCodeRequest request);
        Task<SalesOrderAssemblyCodeRequest?>UpdateAsync(long id,SalesOrderAssemblyCodeRequest request);
        Task<bool> DeleteAsync(long id);
    }
}
