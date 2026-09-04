using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_CK_URUN_DETAY_RENK")]
    public class CKProductDetailColor
    {
        [Column("ID")]
        public string? Id { get; set; }

        [Column("URUN_GRUP_ID")]
        public string? UrunGrupId { get; set; }

        [Column("URUN_TIPI_ID")]
        public string? UrunTipiId { get; set; }

        [Column("URUN_ID")]
        public string? UrunId { get; set; }

        [Column("URUN_ADI")]
        public string? UrunAdi { get; set; }

        [Column("KATALOG_KODU")]
        public string? KatalogKodu { get; set; }

        [Column("AYAK_RAL")]
        public string? AyakRal { get; set; }

        [Column("AYAK_RAL_DETAY")]
        public string? AyakRalDetay { get; set; }

        [Column("GOVDE_KANAT_RAL")]
        public string? GovdeKanatRal { get; set; }

        [Column("GOVDE_KANAT_RAL_DETAY")]
        public string? GovdeKanatRalDetay { get; set; }

        [Column("PLASTIK_RENK_1_NO")]
        public string? PlastikRenk1No { get; set; }

        [Column("PLASTIK_RENK_2_NO")]
        public string? PlastikRenk2No { get; set; }
    }
}