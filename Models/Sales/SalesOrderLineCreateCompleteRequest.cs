using Microsoft.AspNetCore.Http;

namespace GranitWebApi.Models.Sales
{
    public class SalesOrderLineCreateCompleteRequest
    {
        public long SalesOrderId { get; set; }
        public int LineNumber { get; set; }
        public string ProductGroupId { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? CatalogCode { get; set; }
        public string ProductTypeId { get; set; } = null!;
        public string ProductId { get; set; } = null!;
        public string ConfigurationCatalogCode { get; set; } = null!;
        public string FootRal { get; set; } = null!;
        public string BodyWingRal { get; set; } = null!;
        public string PlasticColor1No { get; set; } = null!;
        public string PlasticColor2No { get; set; } = null!;
        public List<IFormFile>? Files { get; set; }
        public List<int>? ImageTypeIds { get; set; }
        public int? KoliId { get; set; }
        public string? KoliKod { get; set; }
        public int? KoliIciMiktar { get; set; }
    }

    public class SalesOrderLineUpdateModel
    {
        public long LineId { get; set; }
        public long SalesOrderId { get; set; }
        public int LineNumber { get; set; }
        public string ProductGroupId { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? CatalogCode { get; set; }
        public string ProductTypeId { get; set; } = null!;
        public string ProductId { get; set; } = null!;
        public string? ConfigurationCatalogCode { get; set; }
        public string? FootRal { get; set; }
        public string? BodyWingRal { get; set; }
        public string? PlasticColor1No { get; set; }
        public string? PlasticColor2No { get; set; }
        public int ConfigurationProductGroupId { get; set; }
        public List<long> ExistingImageIds { get; set; }= new();
        public List<IFormFile> Files { get; set; }= new();
        public List<int> ImageTypeIds { get; set; }= new();
        public int? KoliId { get; set; }
        public string? KoliKod { get; set; }
        public int? KoliIciMiktar { get; set; }
    }
}