using Newtonsoft.Json;

namespace GranitWebApi.Models.Trendyol
{
    public class TrendyolOrder
    {
        public long Id { get; set; }

        public string OrderNumber { get; set; }

        public long CustomerId { get; set; }

        public string CustomerFirstName { get; set; }

        public string CustomerLastName { get; set; }

        public string CustomerEmail { get; set; }

        public long SupplierId { get; set; }

        public long ShipmentPackageId { get; set; }

        public string Status { get; set; }

        public string ShipmentPackageStatus { get; set; }

        public string CargoProviderName { get; set; }

        public string CargoTrackingNumber { get; set; }

        public string CargoTrackingLink { get; set; }

        public string CargoSenderNumber { get; set; }

        public string CurrencyCode { get; set; }

        public decimal PackageTotalPrice { get; set; }

        public decimal PackageGrossAmount { get; set; }

        public decimal PackageSellerDiscount { get; set; }

        public decimal PackageTyDiscount { get; set; }

        public decimal PackageTotalDiscount { get; set; }

        public string InvoiceLink { get; set; }

        public string InvoiceNumber { get; set; }

        public string InvoiceStatus { get; set; }

        public bool Commercial { get; set; }

        public bool Micro { get; set; }

        public bool FastDelivery { get; set; }

        public bool IsCod { get; set; }

        public long OrderDate { get; set; }

        public long LastModifiedDate { get; set; }

        public TrendyolAddress ShipmentAddress { get; set; }

        public TrendyolAddress InvoiceAddress { get; set; }

        public List<TrendyolOrderLine> Lines { get; set; }

        public List<TrendyolPackageHistory> PackageHistories { get; set; }
    }
}