using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Keyless]
    [Table("GRANIT_TBL_URUN_SHRINK")]
    public class ProductShrink
    {
        [Column("KATALOG_KOD")]
        public string KatalogKod { get; set; } = null!;

        [Column("URUN_ADI")]
        public string? UrunAdi { get; set; }

        [Column("SHRINK_KOD")]
        public string ShrinkKod { get; set; } = null!;

        [Column("DURUM")]
        public string? Durum { get; set; }

        [Column("MIKTAR")]
        public decimal Miktar { get; set; }
    }
}