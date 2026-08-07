namespace GranitWebApi.Workflow.Models
{
    public class WorkflowDefinition
    {
        public int Id { get; set; }

        public int ProcessTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ProcessType ProcessType { get; set; } = null!;

        // Workflow adımları
        public ICollection<WorkflowStep> Steps { get; set; }
            = new List<WorkflowStep>();
    }
}