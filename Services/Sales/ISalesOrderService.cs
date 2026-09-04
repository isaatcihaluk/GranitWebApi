using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderService
    {
        Task<SalesOrder> CreateAsync(SalesOrder salesOrder,int userId);
        Task<List<SalesOrder>> GetAllAsync(int userId);
        Task<SalesOrder?> GetByIdAsync(long id);
        Task<SalesOrder?> UpdateAsync(long id,SalesOrder salesOrder,int userId);
        Task<List<SalesOrderLineList>> GetLinesAsync(long salesOrderId);
        Task<SalesOrderErpCheckResult?> CheckErpAsync(long salesOrderId);
        Task<int> SendForApprovalAsync(long salesOrderId, int userId);
        Task CompleteTechnicalAsync(long salesOrderId, int userId);
    }
}