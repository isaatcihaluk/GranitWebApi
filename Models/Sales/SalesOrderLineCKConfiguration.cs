using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderLineCKConfigurations")]
    public class SalesOrderLineCKConfiguration
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public string ProductTypeId { get; set; } = null!;
        public string ProductId { get; set; } = null!;
        public string CatalogCode { get; set; } = null!;
        public string FootRal { get; set; } = null!;
        public string BodyWingRal { get; set; } = null!;
        public string PlasticColor1No { get; set; } = null!;
        public string PlasticColor2No { get; set; } = null!;
        public string? MainAssemblyCode { get; set; }
        public string? ManualAssemblyCode { get; set; }
        public bool MainAssemblyCodeExists { get; set; }
        public bool ManualAssemblyCodeExists { get; set; }
        public string? ProductGroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(SalesOrderLineId))]
        public SalesOrderLine? SalesOrderLine { get; set; }
    }
}