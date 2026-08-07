namespace GranitWebApi.Models.Promanage;

public class PromanageGunlukUretim
{
    public int Id { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Shift { get; set; }
    public string? MachineGroup { get; set; }
    public string? MachineName { get; set; }
    public string? Operator { get; set; }
    public string? JobOrderNo { get; set; }
    public string? StockName { get; set; }
    public decimal TotalProduced { get; set; }
    public decimal GoodProduced { get; set; }
    public string? SourceFileName { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ImportDate { get; set; }
}