using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Trendyol
{
    [Table("TY_ORDER_LINE")]
    public class TyOrderLine
    {
        [Key]
        public int ID { get; set; }

        public int ORDER_HEADER_ID { get; set; }

        public long? LINE_ID { get; set; }

        public string BARCODE { get; set; }

        public string STOCK_CODE { get; set; }

        public string PRODUCT_NAME { get; set; }

        public string PRODUCT_SIZE { get; set; }

        public string PRODUCT_COLOR { get; set; }

        public long? PRODUCT_CATEGORY_ID { get; set; }

        public decimal QUANTITY { get; set; }

        public decimal LINE_UNIT_PRICE { get; set; }

        public decimal LINE_GROSS_AMOUNT { get; set; }

        public decimal LINE_SELLER_DISCOUNT { get; set; }

        public decimal LINE_TY_DISCOUNT { get; set; }

        public decimal LINE_TOTAL_DISCOUNT { get; set; }

        public decimal VAT_RATE { get; set; }

        public decimal COMMISSION { get; set; }

        public string ORDER_LINE_ITEM_STATUS_NAME { get; set; }

        public string CANCELLED_BY { get; set; }

        public string CANCEL_REASON { get; set; }

        public int? CANCEL_REASON_CODE { get; set; }
    }
}