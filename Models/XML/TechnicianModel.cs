using System.ComponentModel.DataAnnotations;

namespace GranitWebApi.Models.XML
{
    public class TechnicianModel
    {
        [Key]
        public int Id { get; set; }
        public int RN { get; set; }
        public int M { get; set; }
        public string? MachineCode { get; set; }
        public string? MachineName { get; set; }
        public int Operator { get; set; }
        public string? OperatorName { get; set; }
        public string? OperatorCode { get; set; }
        public int? Technician { get; set; }
        public string? TechnicianCode { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime? TCall { get; set; }
        public DateTime? TLogin { get; set; }
        public DateTime? TEnd { get; set; }
        public TimeSpan? Duration { get; set; }
        public TimeSpan? ReactionTime { get; set; }
        public int? Shift { get; set; }
        public bool IsDetailed { get; set; } = false;
        public string? Status { get; set; } = "Açık";
        public DateTime? CreatedAt { get; set; }
    }
}
