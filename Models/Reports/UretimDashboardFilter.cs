namespace GranitWebApi.Models.Reports.Uretim
{
    public class UretimDashboardFilter
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // Vardiya
        public string? Shift { get; set; }
        // Makine grubu
        public string? MachineGroup { get; set; }
        // Makine
        public string? MachineName { get; set; }
    }
}