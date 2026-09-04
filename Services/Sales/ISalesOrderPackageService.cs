using GranitWebApi.Models.Sales;

namespace GranitWebApi.Services.Sales
{
    public interface ISalesOrderPackageService
    {
        Task<SalesOrder?> GetPackagingDataAsync(long salesOrderId);
        Task<SalesOrderPackage> CreatePackageAsync(SalesOrderPackageCreateRequest request,int userId);
        Task<SalesOrderPackage> UpdatePackageAsync(long packageId,SalesOrderPackageCreateRequest request,int userId);
        Task CancelPackageAsync(long packageId,long salesOrderId,int userId);
    }
}