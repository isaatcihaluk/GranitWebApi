using Newtonsoft.Json;

namespace GranitWebApi.Models.Trendyol
{
    public class TrendyolOrderLine
    {
        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("vatRate")]
        public decimal VatRate { get; set; }

        [JsonProperty("lineUnitPrice")]
        public decimal LineUnitPrice { get; set; }

        [JsonProperty("lineGrossAmount")]
        public decimal LineGrossAmount { get; set; }

        [JsonProperty("lineSellerDiscount")]
        public decimal LineSellerDiscount { get; set; }

        [JsonProperty("lineTyDiscount")]
        public decimal LineTyDiscount { get; set; }

        [JsonProperty("lineTotalDiscount")]
        public decimal LineTotalDiscount { get; set; }

        [JsonProperty("commission")]
        public decimal Commission { get; set; }

        [JsonProperty("stockCode")]
        public string StockCode { get; set; }

        [JsonProperty("productSize")]
        public string ProductSize { get; set; }

        [JsonProperty("productColor")]
        public string ProductColor { get; set; }

        [JsonProperty("productCategoryId")]
        public long ProductCategoryId { get; set; }

        [JsonProperty("lineId")]
        public long LineId { get; set; }

        [JsonProperty("orderLineItemStatusName")]
        public string OrderLineItemStatusName { get; set; }

        [JsonProperty("cancelledBy")]
        public string CancelledBy { get; set; }

        [JsonProperty("cancelReason")]
        public string CancelReason { get; set; }

        [JsonProperty("cancelReasonCode")]
        public int? CancelReasonCode { get; set; }
    }
}