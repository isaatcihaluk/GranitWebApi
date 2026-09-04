using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_CK_URUN_DETAY_ANA")]
    public class CKProductDetailMain
    {
        [Column("URUN_ID")]
        public string? UrunId { get; set; }

        [Column("URUN_GRUP_ID")]
        public string? UrunGrupId { get; set; }

        [Column("URUN_TIPI_ID")]
        public string? UrunTipiId { get; set; }

        [Column("URUN_ADI")]
        public string? UrunAdi { get; set; }

        [Column("KATALOG_KODU")]
        public string? KatalogKodu { get; set; }

        [Column("GOVDE_DETAY")]
        public string? GovdeDetay { get; set; }

        [Column("KANAT_DETAY")]
        public string? KanatDetay { get; set; }
    }
}