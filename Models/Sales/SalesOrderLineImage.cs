using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderLineImages")]
    public class SalesOrderLineImage
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public int ImageTypeId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public int VersionNo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation

        [ForeignKey(nameof(SalesOrderLineId))]
        [JsonIgnore]
        public SalesOrderLine? SalesOrderLine { get; set; }

        [ForeignKey(nameof(ImageTypeId))]
        public SalesOrderImageType? ImageType { get; set; }
    }
}
