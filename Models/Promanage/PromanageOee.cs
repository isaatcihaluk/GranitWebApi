namespace GranitWebApi.Models.Promanage
{
    public class PromanageOee
    {
        public int Id { get; set; }
        public DateTime ReportDate { get; set; }
        public string? Shift { get; set; }
        public string? MachineGroup { get; set; }
        public string? MachineName { get; set; }
        public decimal Availability { get; set; }
        public decimal Performance { get; set; }
        public decimal Quality { get; set; }
        public decimal Oee { get; set; }
        public string? SourceFileName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ImportDate { get; set; }
    }
}