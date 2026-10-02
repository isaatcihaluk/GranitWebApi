using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Interfaces
{
    public interface IWorkflowProcessHandler
    {
        string ProcessTypeCode { get; }

        Task<WorkflowStepResult> OnStepCompletedAsync(ProcessRequest request,int completedStep,int userId);
        Task OnCompletedAsync(ProcessRequest request,int userId);
        Task OnReturnedAsync(ProcessRequest request,int revisionStep,int userId);
        Task<WorkflowResubmitResult> OnResubmittedAsync(ProcessRequest request,int revisionStep,int userId);
    }
}