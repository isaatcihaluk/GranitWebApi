using GranitWebApi.Data;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Workflow.Resolvers
{
    public class ManagerChainResolver : IApprovalResolver
    {
        private readonly AppDbContext _context;

        public ManagerChainResolver(AppDbContext context)
        {
            _context = context;
        }
        public ApprovalType ApprovalType => ApprovalType.ManagerChain;

        public async Task<List<int>> ResolveAsync(
    WorkflowStep step,
    ProcessRequest request)
        {
            List<int> managers = new();

            HashSet<int> visited = new();

            int? currentUserId = request.CreatedBy;


            while (currentUserId != null)
            {
                var managerId = await _context.Users
                    .Where(x => x.Id == currentUserId)
                    .Select(x => x.ManagerId)
                    .FirstOrDefaultAsync();


                if (managerId == null)
                    break;


                if (!visited.Add(managerId.Value))
                    break;


                managers.Add(managerId.Value);


                currentUserId = managerId;
            }


            // ManagerLevel yoksa hepsini döndür
            if (step.ManagerLevel == null)
                return managers;


            // 1. yönetici, 2. yönetici, 3. yönetici
            var index = step.ManagerLevel.Value - 1;


            if (index < managers.Count)
            {
                return new List<int>
        {
            managers[index]
        };
            }


            return new List<int>();
        }
    }
}