using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_INTEGRATION_LOG")]
    public class TyIntegrationLog
    {
        [Key]
        public int ID { get; set; }

        public string ORDER_NUMBER { get; set; }

        public string LOG_TYPE { get; set; } // INFO / ERROR

        public string MESSAGE { get; set; }

        public DateTime CREATED_DATE { get; set; } = DateTime.Now;
    }
}