using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Interfaces
{
    public interface IWorkflowCompletionHandler
    {
        string ProcessTypeCode { get; }

        Task HandleAsync(ProcessRequest request);
    }
}