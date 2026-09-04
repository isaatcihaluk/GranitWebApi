namespace GranitWebApi.Models.BakimOnarim
{
    public class MaintenanceReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCalls { get; set; }
        public int OpenCalls { get; set; }
        public int InProgressCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int OperatorClosedCalls { get; set; }
        public int DetailedCompletedCalls { get; set; }
        public int UndetailedCompletedCalls { get; set; }
        public double AverageReactionMinutes { get; set; }
        public double AverageInterventionMinutes { get; set; }
        public List<MaintenanceDailyTrendDto> DailyTrend { get; set; } = new();
        public List<MaintenanceTechnicianReportDto> Technicians { get; set; } = new();
        public List<MaintenanceMachineReportDto> Machines { get; set; } = new();
    }

    public class MaintenanceDailyTrendDto
    {
        public DateTime Date { get; set; }
        public int TotalCalls { get; set; }
        public int OpenCalls { get; set; }
        public int InProgressCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int OperatorClosedCalls { get; set; }
        public int DetailedCompletedCalls { get; set; }
        public int UndetailedCompletedCalls { get; set; }
        public double AverageReactionMinutes { get; set; }
        public double AverageInterventionMinutes { get; set; }
    }

    public class MaintenanceTechnicianReportDto
    {
        public string TechnicianCode { get; set; } = "";
        public string TechnicianName { get; set; } = "";
        public int CallCount { get; set; }
        public int CompletedCount { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int OperatorClosedCount { get; set; }
        public double AverageReactionMinutes { get; set; }
        public double AverageInterventionMinutes { get; set; }
        public int DetailedCount { get; set; }
        public int UndetailedCount { get; set; }
    }
    public class MaintenanceMachineReportDto
    {
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public int CallCount { get; set; }
        public int OpenCalls { get; set; }
        public int InProgressCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int OperatorClosedCalls { get; set; }
        public int DetailedCompletedCalls { get; set; }
        public int UndetailedCompletedCalls { get; set; }
        public double AverageReactionMinutes { get; set; }
        public double AverageInterventionMinutes { get; set; }
    }
    public class MaintenanceReportRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TeamFilter { get; set; } = "all";
        public string ShiftFilter { get; set; } = "";
        public string? MachineGroup { get; set; }
        public string? MachineCode { get; set; }
    }
    public class TechnicianUserDto
    {
        public string? Sicil { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UnitName { get; set; }
    }
}