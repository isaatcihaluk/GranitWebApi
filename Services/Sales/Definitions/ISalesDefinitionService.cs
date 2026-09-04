using GranitWebApi.Models.Sales.Definitions;

namespace GranitWebApi.Services.Sales.Definitions
{
    public interface ISalesDefinitionService
    {
        Task<IEnumerable<object>> GetProductGroupsAsync();
        Task<IEnumerable<object>> GetProductTypesAsync(string? urunGrupId);
        Task<IEnumerable<object>> GetCKProductDetailsAsync(string? urunGrupId,string? urunTipiId);
        Task<IEnumerable<object>> GetCKProductDetailColorsAsync(string? katalogKodu,string? ayakRal = null,string? govdeKanatRal = null);
        Task<IEnumerable<object>> GetPlasticColorsAsync();
        Task<IEnumerable<object>> GetSalesOrderImageTypesAsync();
    }
}