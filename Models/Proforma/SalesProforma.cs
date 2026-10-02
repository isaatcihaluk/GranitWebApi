using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Proforma
{
    [Table("SalesProformas")]
    public class SalesProforma
    {
        [Key]
        public int Id { get; set; }
        public int SalesRepresentativeUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProformaNumber { get; set; } = string.Empty;
        public int ProformaYear { get; set; }
        public string SystemProformaNumber { get; set; } = string.Empty;

        [MaxLength(25)]
        public string? SalesType { get; set; }

        [MaxLength(25)]
        public string? OrderType { get; set; }

        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }

        [MaxLength(50)]
        public string? DeliveryMethod { get; set; }
        public int? IncotermId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentType { get; set; } = string.Empty;
        public decimal? AdvancePercentage { get; set; }
        public int? PaymentTermDays { get; set; }

        [MaxLength(1000)]
        public string? PaymentDescription { get; set; }
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "TASLAK";
        public bool Ordered { get; set; } = false;
        // Workflow
        public int? ProcessRequestId { get; set; }

        // Açıklama
        [MaxLength(1000)]
        public string? Description { get; set; }
        // Audit
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Satırlar
        public ICollection<SalesProformaLine> Lines { get; set; } = new List<SalesProformaLine>();
        public ICollection<SalesProformaPayment> Payments { get; set; } = new List<SalesProformaPayment>();
    }
}