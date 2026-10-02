using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderFormService
    {
        Task<SalesOrderForm?> GetOrderFormDataAsync(long salesOrderId);
        Task<byte[]> GenerateOrderFormPdfAsync(long salesOrderId);
    }
}