namespace GranitWebApi.Models.Reports
{
    public class ProductionSummaryResult
    {
        public decimal TotalProduced { get; set; }
        public decimal GoodProduced { get; set; }
        public decimal ScrapAmount { get; set; }
        public decimal QualityRate { get; set; }
        public int TotalRecords { get; set; }
        public int MachineCount { get; set; }
    }
    public class ProductionTrendResult
    {
        public DateTime Date { get; set; }
        public string? Shift { get; set; }
        public decimal TotalProduced { get; set; }
        public decimal GoodProduced { get; set; }
        public decimal ScrapAmount { get; set; }
        public decimal QualityRate { get; set; }
    }
    public class ProductionMachineResult
    {
        public string? MachineName { get; set; }
        public string? MachineGroup { get; set; }
        public decimal TotalProduced { get; set; }
        public decimal GoodProduced { get; set; }
        public decimal ScrapAmount { get; set; }
        public decimal QualityRate { get; set; }
        public int RecordCount { get; set; }
    }
    public class QualitySummaryResult
    {
        public decimal TotalScrap { get; set; }
        public decimal TotalRework { get; set; }
        public decimal TotalFire { get; set; }
        public int TotalRecords { get; set; }
        public int MachineCount { get; set; }
    }
    public class QualityTrendResult
    {
        public DateTime Date { get; set; }
        public int? Shift { get; set; }
        public decimal ScrapAmount { get; set; }
        public decimal ReworkAmount { get; set; }
        public decimal FireAmount { get; set; }
        public int RecordCount { get; set; }
    }
    public class QualityMachineResult
    {
        public string? MachineName { get; set; }
        public string? MachineGroup { get; set; }
        public decimal ScrapAmount { get; set; }
        public decimal ReworkAmount { get; set; }
        public decimal FireAmount { get; set; }
        public int TotalRecords { get; set; }
    }
    public class QualityReasonResult
    {
        public string? ScrapCode { get; set; }
        public string? ScrapName { get; set; }
        public decimal ScrapAmount { get; set; }
        public int TotalRecords { get; set; }
    }
    public class OperatorProductionResult
    {
        public DateTime ReportDate { get; set; }
        public string Shift { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string Operator { get; set; } = "";
        public string StockName { get; set; } = "";
        public decimal TotalProduced { get; set; }
        public decimal GoodProduced { get; set; }
        public decimal QualityRate { get; set; }
    }
}
