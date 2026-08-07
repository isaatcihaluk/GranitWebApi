namespace GranitWebApi.Models.Promanage;

public class PromanageStops
{
    public int Id { get; set; }
    public long RN { get; set; }
    public int? M { get; set; }
    public string? StopCode { get; set; }
    public int? Station { get; set; }
    public string? StationName { get; set; }
    public DateTime? TStart { get; set; }
    public DateTime? TEnd { get; set; }
    public string? Operator { get; set; }
    public long? CStart { get; set; }
    public long? CEnd { get; set; }
    public long? PStart { get; set; }
    public long? PEnd { get; set; }
    public int? Technician { get; set; }
    public string? Notes { get; set; }
    public string? Notes2 { get; set; }
    public string? MachineName { get; set; }
    public string? MachineCode { get; set; }
    public string? MachineGroup { get; set; }
    public string? StopName { get; set; }
    public string? StopDuration { get; set; }
    public decimal? StopMinute { get; set; }
    public int? StopGroup { get; set; }
    public string? StopGroupName { get; set; }
    public string? StopOeeGroup { get; set; }
    public string? OperatorName { get; set; }
    public string? TechnicianName { get; set; }
    public decimal? Kwh { get; set; }
    public int? Shift { get; set; }
    public string? OpNote { get; set; }
    public string? TecNote { get; set; }
    public string? TecNoteEditor { get; set; }
    public DateTime ImportDate { get; set; }
}