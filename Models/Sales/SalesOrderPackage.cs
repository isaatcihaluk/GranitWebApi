using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderPackageLines")]
    public class SalesOrderPackageLine
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderPackageId { get; set; }
        public long SalesOrderLineId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey(nameof(SalesOrderPackageId))]
        public SalesOrderPackage? SalesOrderPackage { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(SalesOrderLineId))]
        public SalesOrderLine? SalesOrderLine { get; set; }
    }

    [Table("SalesOrderPackages")]
    public class SalesOrderPackage
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderId { get; set; }
        public int PackageNumber { get; set; }
        public string KoliId { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string KoliKod { get; set; } = null!;
        public int KoliIciMiktar { get; set; }

        [MaxLength(50)]
        public string? NetsisPaketKodu { get; set; }

        [MaxLength(250)]
        public string? NetsisPaketAdi { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? PackageQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? KoliAdedi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitKoliPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "TASLAK";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? NetsisTransferredAt { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey(nameof(SalesOrderId))]
        public SalesOrder? SalesOrder { get; set; }
        public ICollection<SalesOrderPackageLine> Lines { get; set; } = new List<SalesOrderPackageLine>();

        [MaxLength(100)]
        public string? Referans { get; set; }
        public string? Definition { get; set; }
    }
    public class SalesOrderPackageCreateRequest
    {
        public long SalesOrderId { get; set; }
        public string KoliId { get; set; } = null!;
        public string? Referans { get; set; }
        public List<SalesOrderPackageCreateLineRequest> Lines { get; set; } = new();
    }
    public class SalesOrderPackageCreateLineRequest
    {
        public long SalesOrderLineId { get; set; }
        public decimal Quantity { get; set; }
    }
    public class SalesOrderPricingRequest
    {
        public long SalesOrderId { get; set; }

        public string? Definition { get; set; }

        public List<SalesOrderPackagePricingRequest> Packages { get; set; } = new();
    }

    public class SalesOrderPackagePricingRequest
    {
        public long PackageId { get; set; }

        public decimal UnitPrice { get; set; }

        public string? Definition { get; set; }
    }

    public class SalesOrderRevisionPricingRequest
    {
        public long SalesOrderId { get; set; }
        public List<SalesOrderPackageRevisionPricingRequest> Packages { get; set; } = new();
    }

    public class SalesOrderPackageRevisionPricingRequest
    {
        public long PackageId { get; set; }
        public decimal KoliAdedi { get; set; }
        public decimal UnitPrice { get; set; }
    }
}