using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_ORDER_ADDRESS")]
    public class TyOrderAddress
    {
        [Key]
        public int ID { get; set; }

        public int ORDER_HEADER_ID { get; set; }

        public string ADDRESS_TYPE { get; set; } // Shipment / Invoice

        public long? ADDRESS_ID { get; set; }

        public string FIRST_NAME { get; set; }

        public string LAST_NAME { get; set; }

        public string COMPANY { get; set; }

        public string ADDRESS1 { get; set; }

        public string ADDRESS2 { get; set; }

        public string CITY { get; set; }

        public int? CITY_CODE { get; set; }

        public string DISTRICT { get; set; }

        public int? DISTRICT_ID { get; set; }

        public string POSTAL_CODE { get; set; }

        public string COUNTRY_CODE { get; set; }

        public string NEIGHBORHOOD { get; set; }

        public string PHONE { get; set; }

        public string TAX_OFFICE { get; set; }

        public string TAX_NUMBER { get; set; }

        public string FULL_ADDRESS { get; set; }

        public string FULL_NAME { get; set; }

        public string LATITUDE { get; set; }

        public string LONGITUDE { get; set; }
    }
}