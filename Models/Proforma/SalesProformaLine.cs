using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Proforma
{
    [Table("SalesProformaLines")]
    public class SalesProformaLine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProformaId { get; set; }

        // Ürün
        public int? ProductGroupId { get; set; }

        public int? ProductId { get; set; }

        [MaxLength(100)]
        public string? ProductCode { get; set; }

        [Required]
        [MaxLength(250)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Miktar / fiyat
        public decimal Quantity { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public decimal UnitPrice { get; set; }

        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        // Soft delete
        public bool DeletedFlag { get; set; }

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
}