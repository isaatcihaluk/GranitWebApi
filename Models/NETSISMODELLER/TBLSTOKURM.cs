using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.NETSISMODELLER
{
    [Keyless]
    [Table("TBLSTOKURM")]
    public class TBLSTOKURM
    {
        [Column("MAMUL_KODU")]
        public string? MAMUL_KODU { get; set; }

        [Column("HAM_KODU")]
        public string? HAM_KODU { get; set; }

        [Column("MIKTAR")]
        public decimal? MIKTAR { get; set; }
    }
}