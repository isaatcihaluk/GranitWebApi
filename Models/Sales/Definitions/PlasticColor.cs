using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_PLASTIK_RENK")]
    public class PlasticColor
    {
        [Key]
        [Column("NO")]
        [StringLength(10)]
        public string No { get; set; } = null!;

        [Column("RENK")]
        [StringLength(250)]
        public string? Renk { get; set; }
    }
}