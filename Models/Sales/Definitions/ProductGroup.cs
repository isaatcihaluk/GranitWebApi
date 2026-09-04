using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales.Definitions
{
    [Table("GRANIT_TBL_URUN_GRUBU")]
    public class ProductGroup
    {
        [Key]
        [Column("ID")]
        [StringLength(5)]
        public string Id { get; set; } = null!;

        [Column("URUN_GRUBU")]
        [StringLength(150)]
        public string? UrunGrubu { get; set; }

        [Column("KISATLMASI")]
        [StringLength(10)]
        public string? Kisaltmasi { get; set; }
    }
}