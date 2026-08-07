namespace GranitWebApi.Workflow.Models
{
    public class ProcessType
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; }
            = new List<WorkflowDefinition>();

        public ICollection<ProcessRequest> ProcessRequests { get; set; }
            = new List<ProcessRequest>();
    }
}