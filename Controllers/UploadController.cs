using GranitWebApi.Services.Upload.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IPromanageOeeUploadService _oeeService;
    private readonly IPromanageGunlukUretimUploadService _gunlukUretimService;

    public UploadController(
        IPromanageOeeUploadService oeeService,
        IPromanageGunlukUretimUploadService gunlukUretimService)
    {
        _oeeService = oeeService;
        _gunlukUretimService = gunlukUretimService;
    }

    [HttpPost("promanage/oee")]
    public async Task<IActionResult> UploadOee(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen bir dosya seçiniz.");

            var user = User.Identity?.Name ?? "SYSTEM";

            var result = await _oeeService.UploadAsync(file, user, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = ex.Message,
                InnerMessage = ex.InnerException?.Message,
                InnerInnerMessage = ex.InnerException?.InnerException?.Message,
                Exception = ex.GetType().FullName,
                InnerException = ex.InnerException?.GetType().FullName
            });
        }
    }

    [HttpPost("promanage/gunluk-uretim")]
    public async Task<IActionResult> UploadGunlukUretim(IFormFile file,CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Lütfen bir dosya seçiniz.");

        var user = User.Identity?.Name ?? "SYSTEM";
        var result = await _gunlukUretimService.UploadAsync(file,user,cancellationToken);
        return Ok(result);
    }
}