namespace GranitWebApi.Workflow.Models
{
    public class WorkflowResolver
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string SqlQuery { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}