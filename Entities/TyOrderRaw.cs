using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_ORDER_RAW")]
    public class TyOrderRaw
    {
        [Key]
        public int ID { get; set; }

        public long TRENDYOL_ORDER_ID { get; set; }

        public string ORDER_NUMBER { get; set; }

        public string RAW_JSON { get; set; }

        public DateTime CREATED_DATE { get; set; } = DateTime.Now;
    }
}