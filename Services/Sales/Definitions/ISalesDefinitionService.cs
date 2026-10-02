using GranitWebApi.Models.Sales.Definitions;

namespace GranitWebApi.Services.Sales.Definitions
{
    public interface ISalesDefinitionService
    {
        Task<IEnumerable<object>> GetProductGroupsAsync();
        Task<IEnumerable<object>> GetProductTypesAsync(string? urunGrupId);
        Task<IEnumerable<object>> GetCKProductDetailsAsync(string? urunGrupId, string? urunTipiId);
        Task<IEnumerable<object>> GetCKProductDetailColorsAsync(string? katalogKodu,string? ayakRal = null,string? govdeKanatRal = null);
        Task<IEnumerable<object>> GetPlasticColorsAsync();
        Task<IEnumerable<object>> GetSalesOrderImageTypesAsync();
        Task<IEnumerable<object>> GetUMBodyTypesAsync();
        Task<IEnumerable<object>> GetUMBodiesAsync(string? bodyType);
        Task<IEnumerable<object>> GetUMIroningBoardsAsync(string? bodyCode);
        Task<IEnumerable<object>> GetUMFeetAsync(string? bodyCode,string? ironingBoardCode);
        Task<object?> GetUMProductAsync(string? bodyCode,string? ironingBoardCode,string? footCode);
        Task<IEnumerable<object>> GetUMProductColorsAsync(string? katalogKodu);
        Task<IEnumerable<object>> GetUMFabricsAsync(string? katalogKodu);
        Task<IEnumerable<object>> GetUMSpongesAsync(string? katalogKodu);
        Task<IEnumerable<object>> GetUMFisTypesAsync();
        Task<IEnumerable<object>> GetUMFisCodesAsync(string? fisTip);
        Task<IEnumerable<object>> GetUMPlasticCombinationsAsync(string? kumasKod);

        // YK - YEDEK KILIF
        Task<IEnumerable<object>> GetYKTypesAsync();
        Task<IEnumerable<object>> GetYKFabricsAsync(string? katalogKodu);
    }
}
