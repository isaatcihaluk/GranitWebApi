using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_PACKAGE_HISTORY")]
    public class TyPackageHistory
    {
        [Key]
        public int ID { get; set; }

        public int ORDER_HEADER_ID { get; set; }

        public string STATUS { get; set; }

        public DateTime? CREATED_DATE { get; set; }
    }
}