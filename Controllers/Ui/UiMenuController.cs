using GranitWebApi.Services.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Ui;

[ApiController]
[Route("api/ui")]
[Authorize]
public class UiMenuController : ControllerBase
{
    private readonly IUiMenuService _service;

    public UiMenuController(IUiMenuService service)
    {
        _service = service;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu(CancellationToken cancellationToken)
    {
        var result = await _service.GetMenuAsync(cancellationToken);

        return Ok(result);
    }
}