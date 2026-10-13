using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_BU_URUN_DETAY_ANA")]
    public class BUProductDetail
    {
        [Column("KATALOG_KOD")]
        public string? KatalogKod { get; set; }

        [Column("URUN_GRUP")]
        public string? UrunGrup { get; set; }

        [Column("URUN_ADI")]
        public string? UrunAdi { get; set; }
    }

    [Table("GRANIT_TBL_BU_URUN_DETAY_RENK")]
    public class BUProductDetailColor
    {
        [Column("URUN_GRUP_ID")]
        public string? UrunGrupId { get; set; }

        [Column("KATALOG_KOD")]
        public string? KatalogKod { get; set; }

        [Column("RENK_RAL")]
        public string? RenkRal { get; set; }

        [Column("RENK_ACIKLAMA")]
        public string? RenkAciklama { get; set; }

        [Column("PLASTIK_RAL")]
        public string? PlastikRal { get; set; }

        [Column("PLASTIK_RENK")]
        public string? PlastikRenk { get; set; }
    }

    public class BUStockDefinition
    {
        public string? Kod { get; set; }
        public string? Ad { get; set; }
    }
}
