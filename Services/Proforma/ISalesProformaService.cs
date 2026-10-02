using GranitWebApi.Models.Proforma;
using GranitWebApi.Models.Sales;
using SalesProformaModel =
    GranitWebApi.Models.Proforma.SalesProforma;

namespace GranitWebApi.Services.Proforma
{
    public interface ISalesProformaService
    {
        Task<List<SalesProformaModel>> GetListAsync();
        Task<SalesProformaModel?> GetByIdAsync(int id);
        Task<SalesProformaModel> CreateAsync(SalesProformaModel proforma,int userId);
        Task<SalesProformaModel?> UpdateAsync(int id,SalesProformaModel proforma,int userId);
        Task<int> GetProcessTypeIdAsync();
        Task<bool> UpdateStatusAsync(int id,string status,int userId);
        Task<bool> SetWorkflowAsync(int id,int processRequestId,string status,int userId);
        Task<bool> CreatePaymentAsync(int proformaId,SalesProformaPaymentRequest request,int userId);
        Task<bool> SubmitPaymentForFinanceAsync(int proformaId,int userId);
        Task<SalesOrder?> ConvertToOrderAsync(int proformaId, int userId);
    }
}