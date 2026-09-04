using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_URUN_TIPI")]
    public class ProductType
    {
        [Column("URUN_GRUP_ID")]
        public string? UrunGrupId { get; set; }

        [Column("URUN_TIPI_ID")]
        public string? UrunTipiId { get; set; }

        [Column("URUN_TIPI")]
        public string? UrunTipi { get; set; }
    }
}