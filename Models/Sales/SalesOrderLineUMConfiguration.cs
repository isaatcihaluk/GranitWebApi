using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderLineUMConfigurations")]
    public class SalesOrderLineUMConfiguration
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public string BodyType { get; set; } = null!;
        public string BodyCode { get; set; } = null!;
        public string IroningBoardCode { get; set; } = null!;
        public string FootCode { get; set; } = null!;
        public string BodyIroningRal { get; set; } = null!;
        public string BodyIroningColor { get; set; } = null!;
        public string FootRal { get; set; } = null!;
        public string FootColor { get; set; } = null!;
        public string FabricCode { get; set; } = null!;
        public string FabricName { get; set; } = null!;
        public string SpongeCode { get; set; } = null!;
        public string SpongeName { get; set; } = null!;
        public decimal SpongeQuantity { get; set; }
        public bool HasFis { get; set; }
        public string? FisType { get; set; }
        public string? FisCode { get; set; }
        public string? FisName { get; set; }
        public string PlasticCombinationNo { get; set; } = null!;
        public string PlasticCombinationDescription { get; set; } = null!;
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