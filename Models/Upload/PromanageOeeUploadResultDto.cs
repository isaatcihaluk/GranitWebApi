namespace GranitWebApi.Models.Upload;

public class PromanageOeeUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int InsertedRows { get; set; }
    public int SkippedRows { get; set; }
    public int ErrorRows { get; set; }
}