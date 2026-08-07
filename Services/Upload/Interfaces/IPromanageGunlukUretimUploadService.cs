using GranitWebApi.Models.Upload;

namespace GranitWebApi.Services.Upload.Interfaces;

public interface IPromanageGunlukUretimUploadService
{
    Task<PromanageOeeUploadResult> UploadAsync(IFormFile file,string createdBy,CancellationToken cancellationToken);
}