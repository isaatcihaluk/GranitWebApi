using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderAssemblyCodeRequests")]
    public class SalesOrderAssemblyCodeRequest
    {
        [Key]
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public long ConfigurationId { get; set; }
        public string MainAssemblyCode { get; set; } = null!;
        public string ManualAssemblyCode { get; set; } = null!;
        public string RequestStatus { get; set; } = "BEKLIYOR";
        public DateTime RequestedAt { get; set; }
        public int RequestedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CompletedBy { get; set; }
        public string? Note { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(SalesOrderLineId))]
        public SalesOrderLine? SalesOrderLine { get; set; }

        [ForeignKey(nameof(ConfigurationId))]
        public SalesOrderLineCKConfiguration? Configuration { get; set; }
    }
}