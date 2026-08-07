using System.ComponentModel.DataAnnotations.Schema;


namespace GranitWebApi.Models.BakimOnarim
{
    // 🔧 ISSUE TYPE
    public class TechnicianMaintenanceIssueType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public bool IsActive { get; set; }
    }

    // 🔧 ACTION TYPE
    public class TechnicianMaintenanceActionType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    // ⚠️ ISSUE
    public class TechnicianMaintenanceIssue
    {
        public int Id { get; set; }
        public int LogRN { get; set; }
        public int IssueTypeId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public int? ComponentId { get; set; }
        public TechnicianMaintenanceIssueType IssueType { get; set; }
    }

    // 🔧 ACTION (AYRI CLASS)

    public class TechnicianMaintenanceAction
    {
        public int Id { get; set; }
        public int LogRN { get; set; }
        public int ActionTypeId { get; set; }

        [Column("ActionDescription")]
        public string Description { get; set; }
        public int ?SolutionType { get; set; }
        public DateTime ActionTime { get; set; } = DateTime.Now;
        public TechnicianMaintenanceActionType ActionType { get; set; }
    }

    // 🔩 PART
    [Table("TechnicianMaintenanceUsedParts")]
    public class TechnicianMaintenancePart
    {
        public int Id { get; set; }
        public int LogRN { get; set; }
        public int ActionId { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
    }

    // 📊 STATUS HISTORY (opsiyonel)
    public class TechnicianMaintenanceStatusHistory
    {
        public int Id { get; set; }
        public int LogRN { get; set; }
        public string Status { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
    }

    // 📦 DTOs
    public class PartDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
    }

    public class IssueDto
    {
        public int IssueTypeId { get; set; }
        public string Description { get; set; }
        public int? ComponentId { get; set; }
    }
    public class ActionDto
    {
        public int ActionTypeId { get; set; }
        public string Description { get; set; }
        public int ?SolutionType { get; set; }
    }
    public class SaveAllDto
    {
        public int LogRN { get; set; }
        public IssueDto Issue { get; set; }
        public ActionDto Action { get; set; }
        public List<PartDto> Parts { get; set; } = new();
        public List<int> TechnicianIds { get; set; } = new();
    }
    public class TechnicianLogTechnicians
    {
        public int Id { get; set; }

        public int LogRN { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
    public class ManualMaintenanceCreateDto
    {
        public string MachineCode { get; set; }
        public string MachineName { get; set; }

        public int? Operator { get; set; }
        public string OperatorCode { get; set; }
        public string OperatorName { get; set; }

        public List<int> TechnicianIds { get; set; } = new();

        public string? TechnicianCode { get; set; }
        public string? TechnicianName { get; set; }

        public DateTime TCall { get; set; }
        public DateTime TLogin { get; set; }
        public DateTime TEnd { get; set; }

        public int Shift { get; set; }
    }
}