using GranitWebApi.Models.Sabitler.Makine;

namespace GranitWebApi.Models.Reports.Uretim
{
    public class OeeSummaryResult
    {
        public decimal AverageOee { get; set; }
        public decimal AverageAvailability { get; set; }
        public decimal AveragePerformance { get; set; }
        public decimal AverageQuality { get; set; }
        public int TotalRecords { get; set; }
        public int MachineCount { get; set; }
    }
    public class OeeTrendResult
    {
        public DateTime Date { get; set; }
        public string? Shift { get; set; }
        public decimal AverageOee { get; set; }
        public decimal AverageAvailability { get; set; }
        public decimal AveragePerformance { get; set; }
        public decimal AverageQuality { get; set; }
    }
    public class OeeMachineResult
    {
        public string MachineName { get; set; }
        public decimal AverageOee { get; set; }
        public decimal AverageAvailability { get; set; }
        public decimal AveragePerformance { get; set; }
        public decimal AverageQuality { get; set; }
    }
    public class DowntimeSummaryResult
    {
        public decimal TotalMinutes { get; set; }
        public int TotalStops { get; set; }
        public int MachineCount { get; set; }
    }

    public class DowntimeMachineResult
    {
        public string? MachineName { get; set; }
        public decimal TotalMinutes { get; set; }
        public int StopCount { get; set; }
    }

    public class DowntimeReasonResult
    {
        public string? StopName { get; set; }
        public decimal TotalMinutes { get; set; }
        public int StopCount { get; set; }
    }

    public class DowntimeTrendResult
    {
        public DateTime Date { get; set; }
        public int? Shift { get; set; }
        public decimal TotalMinutes { get; set; }
        public int StopCount { get; set; }
    }
    public class DashboardResult
    {
        // FILTER DATA
        public List<string> MachineGroups { get; set; } = new();
        public List<MachineFilterDto> Machines { get; set; } = new();
        public OeeSummaryResult? OeeSummary { get; set; }
        public List<OeeTrendResult> OeeTrend { get; set; } = new();
        public List<OeeMachineResult> MachineOee { get; set; } = new();

        public DowntimeSummaryResult? DowntimeSummary { get; set; }
        public List<DowntimeMachineResult> DowntimeMachine { get; set; } = new();
        public List<DowntimeReasonResult> DowntimeReason { get; set; } = new();
        public List<DowntimeTrendResult> DowntimeTrend { get; set; } = new();

        public ProductionSummaryResult? ProductionSummary { get; set; }
        public List<ProductionTrendResult> ProductionTrend { get; set; } = new();
        public List<ProductionMachineResult> ProductionMachine { get; set; } = new();

        public QualitySummaryResult? QualitySummary { get; set; }
        public List<QualityTrendResult> QualityTrend { get; set; } = new();
        public List<QualityMachineResult> QualityMachine { get; set; } = new();
        public List<QualityReasonResult> QualityReason { get; set; } = new();
        public List<OperatorProductionResult> OperatorProduction { get; set; } = new();
    }
}