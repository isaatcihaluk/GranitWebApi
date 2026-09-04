using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_URUN_KOLI")]
    public class ProductBox
    {
        [Key]
        [Column("ID")]
        [StringLength(50)]
        public string Id { get; set; } = null!;

        [Column("URUN_GRUP_ID")]
        [StringLength(50)]
        public string? UrunGrupId { get; set; }

        [Column("KATALOG_KOD")]
        [StringLength(50)]
        public string? KatalogKod { get; set; }

        [Column("MUSTERI_KOD")]
        [StringLength(50)]
        public string? MusteriKod { get; set; }

        [Column("MUSTERI_ADI")]
        [StringLength(250)]
        public string? MusteriAdi { get; set; }

        [Column("KOLI_KOD")]
        [StringLength(50)]
        public string? KoliKod { get; set; }

        [Column("KOLI_ICI_MIKTAR")]
        public int? KoliIciMiktar { get; set; }

        [Column("DURUM")]
        [StringLength(50)]
        public string? Durum { get; set; }
    }
}