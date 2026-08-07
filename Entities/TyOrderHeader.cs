using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_ORDER_HEADER")]
    public class TyOrderHeader
    {
        [Key]
        public int ID { get; set; }

        public long TRENDYOL_ORDER_ID { get; set; }

        public string ORDER_NUMBER { get; set; }

        public long? CUSTOMER_ID { get; set; }

        public string CUSTOMER_FIRSTNAME { get; set; }

        public string CUSTOMER_LASTNAME { get; set; }

        public string CUSTOMER_EMAIL { get; set; }

        public long? SUPPLIER_ID { get; set; }

        public long? SHIPMENT_PACKAGE_ID { get; set; }

        public string STATUS { get; set; }

        public string SHIPMENT_PACKAGE_STATUS { get; set; }

        public string CARGO_PROVIDER_NAME { get; set; }

        public string CARGO_TRACKING_NUMBER { get; set; }

        public string CARGO_TRACKING_LINK { get; set; }

        public string CARGO_SENDER_NUMBER { get; set; }

        public string CURRENCY_CODE { get; set; }

        public decimal? PACKAGE_TOTAL_PRICE { get; set; }

        public decimal? PACKAGE_GROSS_AMOUNT { get; set; }

        public decimal? PACKAGE_SELLER_DISCOUNT { get; set; }

        public decimal? PACKAGE_TY_DISCOUNT { get; set; }

        public decimal? PACKAGE_TOTAL_DISCOUNT { get; set; }

        public string INVOICE_LINK { get; set; }

        public string INVOICE_NUMBER { get; set; }

        public string INVOICE_STATUS { get; set; }

        public bool? COMMERCIAL { get; set; }

        public bool? MICRO { get; set; }

        public bool? FAST_DELIVERY { get; set; }

        public bool? IS_COD { get; set; }

        public DateTime? ORDER_DATE { get; set; }

        public DateTime? LAST_MODIFIED_DATE { get; set; }

        public string NETSIS_FISNO { get; set; }

        public int INTEGRATION_STATUS { get; set; } = 0;

        public string ERROR_MESSAGE { get; set; }

        public DateTime CREATED_DATE { get; set; } = DateTime.Now;
    }
}