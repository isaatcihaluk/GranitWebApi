using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Interfaces
{
    public interface IWorkflowService
    {
        Task<int> StartAsync(ProcessRequest request);
        Task SubmitAsync(int requestId,int userId,string? comment);
        Task ApproveAsync(int requestId,int userId,string? comment);
        Task RejectAsync(int requestId,int userId,string? comment);
        Task ReturnAsync(int requestId,int userId,string? comment);
        Task ResubmitAsync(int requestId,int userId,string? comment,string? parameters = null);
        Task CancelAsync(int requestId,int userId,string? comment);
        Task<int> StartAndSubmitAsync(ProcessRequest request,string? comment = null);
        Task ActivateStepAsync(int requestId,int stepOrder,int userId,string? comment = null);
    }
}