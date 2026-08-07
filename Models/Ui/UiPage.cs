using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Ui;
[Table("UiPages")]
public class UiPage
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public int OrderNo { get; set; }
    public bool IsMenu { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedUserId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? UpdatedUserId { get; set; }
    //public ICollection<UiPagePermission> Permissions { get; set; }
    //= new List<UiPagePermission>();
}