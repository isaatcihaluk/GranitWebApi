namespace GranitWebApi.Models.Sales
{
    public class SalesOrderShrinkRecipePrepareResult
    {
        public bool Basarili { get; set; }
        public bool Hata { get; set; }
        public bool NetsisReceteVar { get; set; }
        public string? ShrinkliKod { get; set; }
        public string? Aciklama { get; set; }
        public long? SalesOrderLineId { get; set; }
        public int? LineNumber { get; set; }
        public string? ProductName { get; set; }
    }
    public class SalesOrderPackageRecetePrepareResult
    {
        public bool Basarili { get; set; }
        public bool Hata { get; set; }
        public bool NetsisReceteVar { get; set; }
        public long? SalesOrderPackageId { get; set; }
        public string? PaketKod { get; set; }
        public string? Aciklama { get; set; }
        public int? PaketSira { get; set; }
        public int? NetsisStatus { get; set; }
        public string? NetsisResponse { get; set; }
    }
    public class SalesOrderNetsisTransferResult
    {
        public bool Basarili { get; set; }
        public bool Hata { get; set; }
        public long SalesOrderId { get; set; }
        public string? NetsisSiparisNo { get; set; }
        public int? NetsisStatus { get; set; }
        public string? NetsisResponse { get; set; }
        public string? ErrorDesc { get; set; }

        public string? Aciklama { get; set; }
    }
}