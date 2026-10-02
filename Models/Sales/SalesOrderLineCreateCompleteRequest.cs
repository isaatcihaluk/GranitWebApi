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
        public string? ProductTypeId { get; set; } = null!;
        public string? ProductId { get; set; } = null!;
        public string? ConfigurationCatalogCode { get; set; } = null!;
        public string? FootRal { get; set; } = null!;
        public string? BodyWingRal { get; set; } = null!;
        public string? PlasticColor1No { get; set; } = null!;
        public string? PlasticColor2No { get; set; } = null!;
        public List<IFormFile>? Files { get; set; }
        public List<int>? ImageTypeIds { get; set; }
        public int? KoliId { get; set; }
        public string? KoliKod { get; set; }
        public int? KoliIciMiktar { get; set; }
        //UM
        public string? BodyType { get; set; }
        public string? BodyCode { get; set; }
        public string? IroningBoardCode { get; set; }
        public string? FootCode { get; set; }

        public string? BodyIroningRal { get; set; }
        public string? BodyIroningColor { get; set; }

        public string? FootColor { get; set; }

        public string? FabricCode { get; set; }
        public string? FabricName { get; set; }

        public string? SpongeCode { get; set; }
        public string? SpongeName { get; set; }
        public decimal? SpongeQuantity { get; set; }

        public bool HasFis { get; set; }
        public string? FisType { get; set; }
        public string? FisCode { get; set; }
        public string? FisName { get; set; }

        public string? PlasticCombinationNo { get; set; }
        public string? PlasticCombinationDescription { get; set; }
        public string? YKFabricCode { get;set; }
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
        public string? ProductTypeId { get; set; } = null!;
        public string? ProductId { get; set; } = null!;
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
        // UM
        public string? BodyType { get; set; }
        public string? BodyCode { get; set; }
        public string? IroningBoardCode { get; set; }
        public string? FootCode { get; set; }
        public string? BodyIroningRal { get; set; }
        public string? BodyIroningColor { get; set; }
        public string? FootColor { get; set; }
        public string? FabricCode { get; set; }
        public string? FabricName { get; set; }
        public string? SpongeCode { get; set; }
        public string? SpongeName { get; set; }
        public decimal? SpongeQuantity { get; set; }
        public bool HasFis { get; set; }
        public string? FisType { get; set; }
        public string? FisCode { get; set; }
        public string? FisName { get; set; }
        public string? PlasticCombinationNo { get; set; }
        public string? PlasticCombinationDescription { get; set; }
        public string? YKFabricCode { get; set; }
    }
}