using GranitWebApi.Data;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Workflow.Resolvers
{
    public class UserResolver : IApprovalResolver
    {
        private readonly AppDbContext _context;

        public ApprovalType ApprovalType => ApprovalType.User;
        public UserResolver(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<int>> ResolveAsync(WorkflowStep step,ProcessRequest request)
        {
            if (step.UserId == null) return Task.FromResult(new List<int>());

            return Task.FromResult(
                new List<int>
                {
                    step.UserId.Value
                });
        }
    }
}
