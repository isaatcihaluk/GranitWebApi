using GranitWebApi.Data;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Workflow.Resolvers
{
    public class RoleResolver : IApprovalResolver
    {
        private readonly AppDbContext _context;

        public RoleResolver(AppDbContext context)
        {
            _context = context;
        }


        public ApprovalType ApprovalType => ApprovalType.Role;


        public async Task<List<int>> ResolveAsync(
            WorkflowStep step,
            ProcessRequest request)
        {
            if (step.RoleId == null)
                return new List<int>();


            var users = await _context.Users
                .Where(x =>
                    x.RoleId == step.RoleId &&
                    x.IsDeleted == false)
                .Select(x => x.Id)
                .ToListAsync();


            return users;
        }
    }
}