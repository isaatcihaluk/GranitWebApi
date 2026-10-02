namespace GranitWebApi.Workflow.Models
{
    public class ProcessRequest
    {
        public int Id { get; set; }
        public int ProcessTypeId { get; set; }
        public int CreatedBy { get; set; }
        public string? Title { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsVisibleToHR { get; set; }
        public string? PdfPath { get; set; }
        public int CurrentStep { get; set; }
        public bool IsCompleted { get; set; }
        public int? EntityId { get; set; }
        public string? Parameters { get; set; }
        public ProcessType? ProcessType { get; set; }
        public ICollection<ProcessApproval> Approvals { get; set; }= new List<ProcessApproval>();
        public ICollection<WorkflowHistory> Histories { get; set; }= new List<WorkflowHistory>();
    }
}