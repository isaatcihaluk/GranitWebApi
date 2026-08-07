namespace GranitWebApi.Workflow.Models
{
    public class ProcessApproval
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int ApproverId { get; set; }
        public int WorkflowStepId { get; set; }
        public int StepOrder { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ActionDate { get; set; }
        public string? Comment { get; set; }
        public bool IsActive { get; set; }
        public ProcessRequest? Request { get; set; }
        public WorkflowStep? WorkflowStep { get; set; }
    }
}