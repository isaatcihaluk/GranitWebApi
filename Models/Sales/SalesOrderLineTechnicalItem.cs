using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderLineTechnicalItems")]
    public class SalesOrderLineTechnicalItem
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public long ImageId { get; set; }
        public string StockCode { get; set; } = null!;
        public string? StockName { get; set; }
        public DateTime SelectedAt { get; set; }
        public int SelectedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
        // Navigation
        [ForeignKey(nameof(SalesOrderLineId))]
        public SalesOrderLine? SalesOrderLine { get; set; }
        [ForeignKey(nameof(ImageId))]
        public SalesOrderLineImage? Image { get; set; }
    }

    public class TechnicalStockInfo
    {
        public string STOK_KODU { get; set; } = string.Empty;
        public string STOK_ADI { get; set; } = string.Empty;
        public string GRUP { get; set; } = string.Empty;
    }
}