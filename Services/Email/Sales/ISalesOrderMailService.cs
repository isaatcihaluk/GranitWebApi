namespace GranitWebApi.Services.Email.Sales
{
    public interface ISalesOrderMailService
    {
        Task<SalesOrderMailResult> SendSalesOrderMailAsync(long salesOrderId);
        Task<SalesOrderMailResult> SendRevisionMailAsync(long salesOrderId,string revisionDescription);
    }
}