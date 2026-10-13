using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesProformaDocument")]
    public class SalesProformaDocument
    {
        public long Id { get; set; }
        public long SalesProformaId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
    public class SalesProformaDocumentResult
    {
        public bool Basarili { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public long? DocumentId { get; set; }
        public string? FileName { get; set; }
    }
    public class SalesProformaDocumentInfo
    {
        public long Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
    }
}