namespace GranitWebApi.Workflow.Models
{
    public class WorkflowHistory
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public int ActionType { get; set; }
        public DateTime ActionDate { get; set; }
        public string? Comment { get; set; }
        public ProcessRequest? Request { get; set; }
    }
}