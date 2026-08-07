using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Ui;

[Table("UiPagePermissions")]
public class UiPagePermission
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int? RoleId { get; set; }
    public int? DepartmentId { get; set; }
    public string AccessType { get; set; } = null!;
    public DateTime? CreatedDate { get; set; }
    // Navigation
    //public UiPage? Page { get; set; }
}