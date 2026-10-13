namespace GranitWebApi.Services.Email.Sales
{
    public class SalesOrderMailResult
    {
        public bool Basarili { get; set; }
        public bool Hata { get; set; }
        public long SalesOrderId { get; set; }
        public string? Aciklama { get; set; }
    }
    public class SalesOrderRevisionMailRequest
    {
        public string RevisionDescription { get; set; } = string.Empty;
    }
    public class SalesOrderMailRecipients
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
    }
}