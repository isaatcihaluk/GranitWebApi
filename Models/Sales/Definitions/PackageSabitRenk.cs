using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_SIPARIS_PAKET_RENKLER")]
    public class PaketRenk
    {
        [Key]
        [Column("NO")]
        [MaxLength(4)]
        public string No { get; set; } = null!;

        [Column("ISIM")]
        [MaxLength(50)]
        public string? Isim { get; set; }

        [Column("RAL1")]
        [MaxLength(50)]
        public string? Ral1 { get; set; }

        [Column("RAL2")]
        [MaxLength(50)]
        public string? Ral2 { get; set; }
    }
}