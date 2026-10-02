namespace GranitWebApi.Services.Email.Sales
{
    public class SalesOrderMailResult
    {
        public bool Basarili { get; set; }
        public bool Hata { get; set; }
        public long SalesOrderId { get; set; }
        public string? Aciklama { get; set; }
    }
    public class FileStorageSettings
    {
        public string RootPath { get; set; } = string.Empty;
    }
}