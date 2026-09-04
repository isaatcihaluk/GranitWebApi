using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderLines")]
    public class SalesOrderLine
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderId { get; set; }
        public int LineNumber { get; set; }
        public string ProductGroupId { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? CatalogCode { get; set; }
        public string? MainAssemblyCode { get; set; }
        public string? ManualAssemblyCode { get; set; }
        public bool? MainAssemblyControl { get; set; }
        public int? KoliId { get; set; }
        public string? KoliKod { get; set; }
        public int? KoliIciMiktar { get; set; }
        public string Status { get; set; } = "TASLAK";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public bool DeletedFlag { get; set; }
        public string? ShrinkliKod { get; set; }
        public string? ShrinkliAd { get; set; }
        [JsonIgnore]
        [ForeignKey(nameof(SalesOrderId))]
        public GranitWebApi.Models.Sales.SalesOrder? SalesOrder { get; set; }
        public SalesOrderLineCKConfiguration? CKConfiguration { get; set; }
        public ICollection<SalesOrderLineImage>? Images { get; set; }
        public ICollection<SalesOrderLineTechnicalItem>? TechnicalItems { get; set; }
        [JsonIgnore]
        public ICollection<SalesOrderPackageLine> PackageLines { get; set; } = new List<SalesOrderPackageLine>();
    }

    public class SalesOrderLineEditResult
    {
        public long LineId { get; set; }
        public long SalesOrderId { get; set; }
        public int LineNumber { get; set; }
        public string ProductGroupId { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? CatalogCode { get; set; }
        public string ProductTypeId { get; set; } = null!;
        public string ProductId { get; set; } = null!;
        public string ConfigurationCatalogCode { get; set; } = null!;
        public string FootRal { get; set; } = null!;
        public string BodyWingRal { get; set; } = null!;
        public string PlasticColor1No { get; set; } = null!;
        public string PlasticColor2No { get; set; } = null!;
        public string? MainAssemblyCode { get; set; }
        public string? ManualAssemblyCode { get; set; }
        public bool MainAssemblyCodeExists { get; set; }
        public bool ManualAssemblyCodeExists { get; set; }
        public List<SalesOrderLineEditImage> Images { get; set; } = new();
        public int? KoliId { get; set; }
        public string? KoliKod { get; set; }
        public int? KoliIciMiktar { get; set; }
    }

    public class SalesOrderLineEditImage
    {
        public long Id { get; set; }
        public int ImageTypeId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public int VersionNo { get; set; }
    }
    public class SalesOrderErpCheckResult
    {
        public bool AllAvailable { get; set; }
        public int TotalLines { get; set; }
        public int AvailableLines { get; set; }
        public List<SalesOrderErpMissingLine> MissingLines { get; set; } = new();

    }
    public class SalesOrderErpMissingLine
    {
        public int LineNumber { get; set; }
        public string? ProductName { get; set; }
        public string? MainAssemblyCode { get; set; }
        public string? ManualAssemblyCode { get; set; }
    }
}