using GranitWebApi.Models.Sales;

public interface ISalesOrderPackageService
{
    Task<SalesOrder?> GetPackagingDataAsync(long salesOrderId);
    Task<SalesOrderPackage> CreatePackageAsync(SalesOrderPackageCreateRequest request);
    Task<SalesOrderPackage> UpdatePackageAsync(long packageId,SalesOrderPackageCreateRequest request);
    Task CancelPackageAsync(long packageId,long salesOrderId);
    Task CompletePackagingAsync(long salesOrderId);
    Task<SalesOrder?> GetPricingDataAsync(long salesOrderId);
    Task SavePricingAsync(SalesOrderPricingRequest request);
    Task UpdateRevisionPricingAsync(SalesOrderRevisionPricingRequest request);
}