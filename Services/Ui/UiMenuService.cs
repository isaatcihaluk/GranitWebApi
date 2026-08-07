using GranitWebApi.Data;
using GranitWebApi.Models.Ui;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GranitWebApi.Services.Ui;

public class UiMenuService : IUiMenuService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public UiMenuService(AppDbContext db,IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<List<UiMenuItemDto>> GetMenuAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) throw new Exception("Kullanıcı bilgisi bulunamadı.");
        var userId = int.Parse(userIdClaim.Value);

        // Kullanıcının rolleri
        var roleIds = await _db.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        // Kullanıcının erişebildiği sayfalar
        var allowedPageIds = await _db.UiPagePermissions
            .Where(x =>
                x.AccessType == "PUBLIC" ||
                (
                    x.AccessType == "ROLE" &&
                    x.RoleId.HasValue &&
                    roleIds.Contains(x.RoleId.Value)
                ))
            .Select(x => x.PageId)
            .Distinct()
            .ToListAsync(cancellationToken);


        if (!allowedPageIds.Any()) return new List<UiMenuItemDto>();

        // Menüleri getir
        var pages = await _db.UiPages
            .Where(x =>
                x.IsActive &&
                x.IsMenu &&
                allowedPageIds.Contains(x.Id))
            .Select(x => new UiMenuItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Code = x.Code,
                Name = x.Name,
                Route = x.Route,
                Icon = x.Icon,
                OrderNo = x.OrderNo
            })
            .OrderBy(x => x.OrderNo)
            .ToListAsync(cancellationToken);

        return BuildTree(pages, null);
    }

    private List<UiMenuItemDto> BuildTree(List<UiMenuItemDto> pages,int? parentId)
    {
        return pages
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.OrderNo)
            .Select(x => new UiMenuItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Code = x.Code,
                Name = x.Name,
                Route = x.Route,
                Icon = x.Icon,
                OrderNo = x.OrderNo,
                Children = BuildTree(pages,x.Id)
            }).ToList();
    }
}