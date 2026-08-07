namespace GranitWebApi.Models.Promanage;

public class PromanageScrap
{
    public int Id { get; set; }
    public long RN { get; set; }
    public int? M { get; set; }
    public string? MachineCode { get; set; }
    public string? MachineName { get; set; }
    public string? MachineGroup { get; set; }
    public long? JobRN { get; set; }
    public int? Station { get; set; }
    public decimal? ScrapAmount { get; set; }
    public DateTime? ScrapDate { get; set; }
    public string? RecordType { get; set; }
    public string? ScrapCode { get; set; }
    public string? ScrapName { get; set; }
    public string? Operator { get; set; }
    public string? OperatorName { get; set; }
    public DateTime? TEnd { get; set; }
    public string? Technician { get; set; }
    public string? TechnicianName { get; set; }
    public int? Shift { get; set; }
    public string? Notes { get; set; }
    public string? Notes2 { get; set; }
    public DateTime ImportDate { get; set; }
}