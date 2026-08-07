using GranitWebApi.Models.Ui;

namespace GranitWebApi.Services.Ui;

public interface IUiMenuService
{
    Task<List<UiMenuItemDto>> GetMenuAsync(CancellationToken cancellationToken);
}