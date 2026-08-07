using GranitWebApi.Workflow.Enums;

namespace GranitWebApi.Workflow.Models
{
    public class WorkflowStep
    {
        public int Id { get; set; }

        public int WorkflowDefinitionId { get; set; }

        public int StepOrder { get; set; }

        public ApprovalType ApprovalType { get; set; }

        public WorkflowStepType StepType { get; set; }

        public int? RoleId { get; set; }

        public int? UserId { get; set; }

        public string? ResolverValue { get; set; }

        public int? ManagerLevel { get; set; }

        public string? Parameters { get; set; }

        public bool IsRequired { get; set; } = true;

        public string? Description { get; set; }

        public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    }
}