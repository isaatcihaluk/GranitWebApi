using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Resolvers
{
    public class NoneResolver : IApprovalResolver
    {
        public ApprovalType ApprovalType => ApprovalType.None;

        public Task<List<int>> ResolveAsync(
            WorkflowStep step,
            ProcessRequest request)
        {
            return Task.FromResult(new List<int>());
        }
    }
}