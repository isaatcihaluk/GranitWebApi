using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Proforma
{
    [Table("SalesProformaPayments")]
    public class SalesProformaPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProformaId { get; set; }

        // Bu ödeme için beklenen tutar
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedAmount { get; set; }

        // Gerçekte gelen / bildirilen ödeme
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaidAmount { get; set; }

        // Para birimi
        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } = string.Empty;

        // Ödemenin gerçekleştiği tarih
        public DateTime? PaymentDate { get; set; }

        // Ödeme açıklaması
        [MaxLength(1000)]
        public string? PaymentDescription { get; set; }

        // BEKLIYOR
        // ODEME_BILDIRILDI
        // FINANS_ONAYINDA
        // ONAYLANDI
        // REDDEDILDI
        // IPTAL
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "BEKLIYOR";

        // Audit
        public DateTime CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation
        [ForeignKey(nameof(ProformaId))]
        [JsonIgnore]
        public SalesProforma? Proforma { get; set; }
    }

    public class SalesProformaPaymentRequest
    {
        public decimal PaidAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentDescription { get; set; }
    }
}