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

        public async Task<List<int>> ResolveAsync(WorkflowStep step,ProcessRequest request)
        {
            if (step.RoleId == null) return new List<int>();

            // Users.RoleId üzerinden gelen kullanıcılar
            var usersByUserRoleField = await _context.Users
                .Where(x =>
                    x.RoleId == step.RoleId &&
                    x.IsDeleted == false)
                .Select(x => x.Id)
                .ToListAsync();

            // UserRoles tablosu üzerinden gelen kullanıcılar
            var usersByUserRoles = await _context.UserRoles
                .Where(x => x.RoleId == step.RoleId)
                .Select(x => x.UserId)
                .ToListAsync();

            // İki kaynağı birleştir, tekrar eden kullanıcıları kaldır
            var users = usersByUserRoleField
                .Union(usersByUserRoles)
                .Distinct()
                .ToList();

            return users;
        }
    }
}