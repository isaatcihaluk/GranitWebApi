using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Interfaces
{
    public interface IApprovalResolver
    {
        // Hangi ApprovalType'ı çözdüğünü belirtir.
        ApprovalType ApprovalType { get; }
        // WorkflowStep'e göre onaylayacak kullanıcıların Id listesini döndürür.
        Task<List<int>> ResolveAsync(
            WorkflowStep step,
            ProcessRequest request);
    }
}