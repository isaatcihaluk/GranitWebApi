using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GranitWebApi.Controllers.Sales
{
    [ApiController]
    [Route("api/sales")]
    [Authorize]
    public class SalesOrderPackageController : ControllerBase
    {
        private readonly ISalesOrderPackageService _packageService;

        public SalesOrderPackageController(
            ISalesOrderPackageService packageService)
        {
            _packageService = packageService;
        }


        // ---------------------------------------------------------
        // PAKETLEME VERİLERİ
        // ---------------------------------------------------------

        [HttpGet("orders/{salesOrderId}/packaging")]
        public async Task<IActionResult> GetPackagingData(
            long salesOrderId)
        {
            var order = await _packageService
                .GetPackagingDataAsync(salesOrderId);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Sipariş bulunamadı."
                });
            }

            return Ok(order);
        }


        // ---------------------------------------------------------
        // PAKET OLUŞTUR
        // ---------------------------------------------------------

        [HttpPost("orders/{salesOrderId}/packaging")]
        public async Task<IActionResult> CreatePackage(
            long salesOrderId,
            [FromBody] SalesOrderPackageCreateRequest request)
        {
            try
            {
                if (salesOrderId != request.SalesOrderId)
                {
                    return BadRequest(new
                    {
                        message =
                            "Sipariş numarası ile gönderilen sipariş bilgisi uyuşmuyor."
                    });
                }


                var userIdClaim = User.FindFirst(
                    ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Kullanıcı bilgisi alınamadı."
                    });
                }


                if (!int.TryParse(
                        userIdClaim.Value,
                        out var userId))
                {
                    return Unauthorized(new
                    {
                        message = "Geçersiz kullanıcı bilgisi."
                    });
                }


                var package =
                    await _packageService.CreatePackageAsync(
                        request,
                        userId);


                return Ok(new
                {
                    package.Id,
                    package.SalesOrderId,
                    package.PackageNumber,
                    package.KoliId,
                    package.KoliKod,
                    package.KoliIciMiktar,
                    package.NetsisPaketKodu,
                    package.NetsisPaketAdi,
                    package.Status,
                    package.CreatedAt,
                    package.CreatedBy,

                    Lines = package.Lines.Select(x => new
                    {
                        x.Id,
                        x.SalesOrderLineId,
                        x.Quantity,
                        x.CreatedAt,
                        x.CreatedBy
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        // ---------------------------------------------------------
        // PAKET GÜNCELLE
        // ---------------------------------------------------------

        [HttpPut("orders/{salesOrderId}/packaging/{packageId}")]
        public async Task<IActionResult> UpdatePackage(
            long salesOrderId,
            long packageId,
            [FromBody] SalesOrderPackageCreateRequest request)
        {
            try
            {
                if (salesOrderId != request.SalesOrderId)
                {
                    return BadRequest(new
                    {
                        message =
                            "Sipariş numarası ile gönderilen sipariş bilgisi uyuşmuyor."
                    });
                }


                var userIdClaim = User.FindFirst(
                    ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Kullanıcı bilgisi alınamadı."
                    });
                }


                if (!int.TryParse(
                        userIdClaim.Value,
                        out var userId))
                {
                    return Unauthorized(new
                    {
                        message = "Geçersiz kullanıcı bilgisi."
                    });
                }


                var package =
                    await _packageService.UpdatePackageAsync(
                        packageId,
                        request,
                        userId);


                return Ok(new
                {
                    package.Id,
                    package.SalesOrderId,
                    package.PackageNumber,
                    package.KoliId,
                    package.KoliKod,
                    package.KoliIciMiktar,
                    package.NetsisPaketKodu,
                    package.NetsisPaketAdi,
                    package.Status,
                    package.UpdatedAt,
                    package.UpdatedBy,

                    Lines = package.Lines.Select(x => new
                    {
                        x.Id,
                        x.SalesOrderLineId,
                        x.Quantity,
                        x.CreatedAt,
                        x.CreatedBy
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        // ---------------------------------------------------------
        // PAKET İPTAL
        // ---------------------------------------------------------

        [HttpDelete("orders/{salesOrderId}/packaging/{packageId}")]
        public async Task<IActionResult> CancelPackage(
            long salesOrderId,
            long packageId)
        {
            try
            {
                var userIdClaim = User.FindFirst(
                    ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Kullanıcı bilgisi alınamadı."
                    });
                }


                if (!int.TryParse(
                        userIdClaim.Value,
                        out var userId))
                {
                    return Unauthorized(new
                    {
                        message = "Geçersiz kullanıcı bilgisi."
                    });
                }


                await _packageService.CancelPackageAsync(
                    packageId,
                    salesOrderId,
                    userId);


                return Ok(new
                {
                    message = "Paket başarıyla iptal edildi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}