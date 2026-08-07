namespace GranitWebApi.Models.Ui;

public class UiMenuItemDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public int OrderNo { get; set; }
    public List<UiMenuItemDto> Children { get; set; } = new();
}