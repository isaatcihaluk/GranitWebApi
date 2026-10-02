using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [ApiController]
    [Route("api/sales")]
    [Authorize]
    public class SalesOrderPackageController : ControllerBase
    {
        private readonly ISalesOrderPackageService _packageService;
        public SalesOrderPackageController(ISalesOrderPackageService packageService)
        {
            _packageService = packageService;
        }

        // PAKETLEME VERİLERİ
        [HttpGet("orders/{salesOrderId}/packaging")]
        public async Task<IActionResult> GetPackagingData(long salesOrderId)
        {
            var order = await _packageService.GetPackagingDataAsync(salesOrderId);
            if (order == null)
            {
                return NotFound(new
                {
                    message = "Sipariş bulunamadı."
                });
            }
            return Ok(order);
        }

        // PAKET OLUŞTUR
        [HttpPost("orders/{salesOrderId}/packaging")]
        public async Task<IActionResult> CreatePackage(long salesOrderId,[FromBody] SalesOrderPackageCreateRequest request)
        {
            try
            {
                if (salesOrderId != request.SalesOrderId)
                {
                    return BadRequest(new
                    {
                        message ="Sipariş numarası ile gönderilen sipariş bilgisi uyuşmuyor."
                    });
                }

                var package =await _packageService.CreatePackageAsync(request);
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
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
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

        // PAKET GÜNCELLE
        [HttpPut("orders/{salesOrderId}/packaging/{packageId}")]
        public async Task<IActionResult> UpdatePackage(long salesOrderId,long packageId,
            [FromBody] SalesOrderPackageCreateRequest request)
        {
            try
            {
                if (salesOrderId != request.SalesOrderId)
                {
                    return BadRequest(new
                    {
                        message ="Sipariş numarası ile gönderilen sipariş bilgisi uyuşmuyor."
                    });
                }
                var package =await _packageService.UpdatePackageAsync(packageId,request);
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
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
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

        // PAKET İPTAL
        [HttpDelete("orders/{salesOrderId}/packaging/{packageId}")]
        public async Task<IActionResult> CancelPackage(long salesOrderId,long packageId)
        {
            try
            {
                await _packageService.CancelPackageAsync(packageId,salesOrderId);
                return Ok(new
                {
                    message = "Paket başarıyla iptal edildi."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
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

        // PAKETLEMEYİ TAMAMLA
        [HttpPost("orders/{salesOrderId}/packaging/complete")]
        public async Task<IActionResult> CompletePackaging(long salesOrderId)
        {
            try
            {
                await _packageService.CompletePackagingAsync(salesOrderId);
                return Ok(new { message = "Paketleme başarıyla tamamlandı." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // FİYATLANDIRMA VERİLERİ
        [HttpGet("{salesOrderId}/pricing")]
        public async Task<IActionResult> GetPricingData(long salesOrderId)
        {
            try
            {
                var result = await _packageService.GetPricingDataAsync(salesOrderId);
                if (result == null)
                {
                    return NotFound(new { message = "Sipariş bulunamadı." });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("orders/pricing")]
        public async Task<IActionResult> SavePricing([FromBody] SalesOrderPricingRequest request)
        {
            try
            {
                await _packageService.SavePricingAsync(request);
                return Ok(new { message = "Fiyatlandırma başarıyla kaydedildi." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("orders/{id:long}/revision-pricing")]
        public async Task<IActionResult> UpdateRevisionPricing(long id,[FromBody] SalesOrderRevisionPricingRequest request)
        {
            try
            {
                request.SalesOrderId = id;
                await _packageService.UpdateRevisionPricingAsync(request);
                return Ok(new
                {
                    message = "Fiyat / miktar revizyonu başarıyla kaydedildi."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
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