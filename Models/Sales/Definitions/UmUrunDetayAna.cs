using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_UM_URUN_DETAY_ANA")]
    public class UmUrunDetayAna
    {
        [Key]
        public string KatalogKodu { get; set; } = null!;
        public int? UrunGrup { get; set; }
        public string? UrunAdi { get; set; }
        public string? GovdeKodu { get; set; }
        public string? UtulukKodu { get; set; }
        public string? GUNo { get; set; }
        public string? AyakKodu { get; set; }
        public string? AyakNo { get; set; }
        public string? GovdeEn { get; set; }
        public string? GovdeBoy { get; set; }
        public string? SungerEn { get; set; }
        public string? SungerBoy { get; set; }
        public string? KumasEn { get; set; }
        public string? KumasBoy { get; set; }
        public string? Antenli { get; set; }
        public string? AntenKod { get; set; }
    }
}
