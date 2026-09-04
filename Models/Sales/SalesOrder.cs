using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrders")]
    public class SalesOrder
    {
        [Key]
        public long Id { get; set; }
        public int SalesRepresentativeUserId { get; set; }

        [Required]
        [MaxLength(25)]
        public string OrderNumber { get; set; } = null!;
        public int OrderYear { get; set; }
        
        [MaxLength(15)]
        public string? SystemOrderNumber { get; set; } = null!;

        [Required]
        [MaxLength(25)]
        public string SalesType { get; set; } = null!;

        [Required]
        [MaxLength(25)]
        public string OrderType { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string CustomerName { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string DeliveryMethod { get; set; } = null!;
        public int? IncotermId { get; set; }
        public DateTime? DueDate { get; set; }

        [MaxLength(10)]
        public string? CurrencyCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "DRAFT";

        public DateTime CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? FinalApprovedAt { get; set; }
        public int? FinalApprovedBy { get; set; }
        [MaxLength(50)]
        public string? NetsisOrderNumber { get; set; }
        public DateTime? NetsisTransferredAt { get; set; }
        public int? ProcessRequestId { get; set; }
        // Navigation Properties
        public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
        public ICollection<SalesOrderPackage> Packages { get; set; } = new List<SalesOrderPackage>();
    }

    public class SalesOrderLineList
    {
        public int Line { get; set; }
        public long Id { get; set; }
        public string ProductGroupId { get; set; } = null!;
        public string? CatalogCode { get; set; }
        public string? ProductName { get; set; }
        public string? Renk { get; set; }
        public string? Plastik { get; set; }
        public decimal Quantity { get; set; }
        public string? MainAssemblyCode { get; set; }
        public string? ManualAssemblyCode { get; set; }
        public bool? MainAssemblyControl { get; set; }
        public string? ShrinkliKod { get; set; }

        // İstersen kontrol amaçlı bunları da döndürebiliriz
        public string? ConfigurationMainAssemblyCode { get; set; }
        public string? ConfigurationManualAssemblyCode { get; set; }
    }

    [Keyless]
    [Table("granitVW_SALES_CUSTOMERS")]
    public class SalesCustomer
    {
        [Column("FIRMA_KOD")]
        public string? FirmaKod { get; set; }

        [Column("MUSTERI")]
        public string? Musteri { get; set; }

        [Column("ADRES")]
        public string? Adres { get; set; }

        [Column("SIRKET")]
        public string? Sirket { get; set; }

        [Column("DOVIZ_TUR")]
        public string? DovizTur { get; set; }
    }
}