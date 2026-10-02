using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    public class SalesOrderForm
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public int OrderYear { get; set; }
        public string? SystemOrderNumber { get; set; }
        public string? SalesType { get; set; }
        public string? OrderType { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? DeliveryMethod { get; set; }
        public int? IncotermId { get; set; }
        public string? Incoterm { get; set; }
        public DateTime? DueDate { get; set; }
        public string? CurrencyCode { get; set; }
        public string? SalesRepresentativeName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Definition { get; set; }
        // PAKETLER / PDF KALEMLERİ
        public List<SalesOrderFormPackage> Packages { get; set; } = new();
    }

    // PAKET
    // PDF'de ana kalem olarak gösterilecek
    public class SalesOrderFormPackage
    {
        public long Id { get; set; }
        public int PackageNumber { get; set; }
        public string KoliId { get; set; } = null!;
        public string KoliKod { get; set; } = null!;
        public int KoliIciMiktar { get; set; }
        public string? KoliDurum { get; set; }
        public string? NetsisPaketKodu { get; set; }
        public string? NetsisPaketAdi { get; set; }
        public string? Referans { get; set; }
        public decimal Miktar { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? PackageQuantity { get; set; }
        public decimal? KoliAdedi { get; set; }
        public decimal? UnitKoliPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Definition { get; set; }
        public SalesOrderFormCKDetails? CKDetails { get; set; }
        public SalesOrderFormUMDetails? UMDetails { get; set; }
        public List<SalesOrderFormPackageLine> Lines { get; set; } = new();
    }

    // PAKET İÇERİĞİNDEKİ SİPARİŞ KALEMİ
    public class SalesOrderFormPackageLine
    {
        public long SalesOrderLineId { get; set; }

        public decimal Quantity { get; set; }

        // PDF görsel bölümünde hangi ürünün kullanılacağını
        // belirlemek için
        public string? ProductName { get; set; }

        public string? CatalogCode { get; set; }

        public string? ShrinkliKod { get; set; }

        public string? ShrinkliAd { get; set; }

        public List<SalesOrderFormImage> Images { get; set; } = new();
    }

    // CK PAKET DETAYLARI
    public class SalesOrderFormCKDetails
    {
        public string? GovdeDetay { get; set; }
        public string? KanatDetay { get; set; }
        public string? AyakRal { get; set; }
        public string? AyakRalDetay { get; set; }
        public string? GovdeKanatRal { get; set; }
        public string? GovdeKanatRalDetay { get; set; }
        public string? PlastikRenk1No { get; set; }
        public string? PlastikRenk1 { get; set; }
        public string? PlastikRenk2No { get; set; }
        public string? PlastikRenk2 { get; set; }
        public string? ShrinkKod { get; set; }
        public string? ShrinkUrunAdi { get; set; }
        public string? ShrinkDurum { get; set; }
        public decimal? ShrinkMiktar { get; set; }
        public List<SalesOrderFormImage> BrosurImages { get; set; } = new();
    }
    public class SalesOrderFormUMDetails
    {
        public string? Govde { get; set; }
        public string? Utuuluk { get; set; }
        public string? Ayak { get; set; }
        public string? Fis { get; set; }
        public string? Anten { get; set; }
        public string? Boya { get; set; }
        public string? PlastikRenk { get; set; }
        public string? Kumas { get; set; }
        public string? Sunger { get; set; }

        public List<SalesOrderFormImage> BrosurImages { get; set; } = new();

        public string? ShrinkKod { get; set; }
        public string? ShrinkAd { get; set; }
        public string? ShrinkDurum { get; set; }
        public decimal? ShrinkMiktar { get; set; }
    }

    // KULLANICI TARAFINDAN YÜKLENEN GÖRSEL / DOSYA
    public class SalesOrderFormImage
    {
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public int ImageTypeId { get; set; }
        public string? ImageTypeCode { get; set; }
        public string? ImageTypeName { get; set; }
        public string FileName { get; set; } = null!;
        public int VersionNo { get; set; }
        [JsonIgnore]
        public string? FilePath { get; set; }
    }
}