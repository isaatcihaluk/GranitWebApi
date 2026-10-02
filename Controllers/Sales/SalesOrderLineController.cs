using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderLineController : ControllerBase
    {
        private readonly ISalesOrderLineService _salesOrderLineService;
        private readonly IConfiguration _configuration;

        public SalesOrderLineController(ISalesOrderLineService salesOrderLineService, IConfiguration configuration)
        {
            _salesOrderLineService = salesOrderLineService;
            _configuration = configuration;
        }

        [HttpGet("order/{salesOrderId:long}")]
        public async Task<IActionResult> GetBySalesOrderId(long salesOrderId)
        {
            try
            {
                var lines =
                    await _salesOrderLineService
                        .GetBySalesOrderIdAsync(salesOrderId);

                return Ok(lines);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var line =
                    await _salesOrderLineService
                        .GetByIdAsync(id);

                if (line == null)
                {
                    return NotFound(new
                    {
                        message = "Sipariş satırı bulunamadı."
                    });
                }

                return Ok(line);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesOrderLine line)
        {
            try
            {
                var result =
                    await _salesOrderLineService
                        .CreateAsync(line);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("create-complete")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateComplete([FromForm] SalesOrderLineCreateCompleteRequest request)
        {
            try
            {
                if (request.Files != null &&
                    request.ImageTypeIds != null &&
                    request.Files.Count != request.ImageTypeIds.Count)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Görsel dosyaları ile görsel tipleri eşleşmiyor."
                    });
                }

                // 1. SATIR
                var line = new SalesOrderLine
                {
                    SalesOrderId = request.SalesOrderId,
                    LineNumber = request.LineNumber,
                    ProductGroupId = request.ProductGroupId,
                    Quantity = request.Quantity,
                    ProductName = request.ProductName,
                    CatalogCode = request.CatalogCode,
                    YKFabricCode = request.YKFabricCode,
                    KoliId = request.KoliId,
                    KoliKod = request.KoliKod,
                    KoliIciMiktar = request.KoliIciMiktar
                };

                // 2. CONFIGURATION
                SalesOrderLineCKConfiguration? ckConfiguration = null;
                SalesOrderLineUMConfiguration? umConfiguration = null;

                // CK
                if (request.ProductGroupId == "1")
                {
                    ckConfiguration =
                        new SalesOrderLineCKConfiguration
                        {
                            ProductTypeId =request.ProductTypeId,
                            ProductId =request.ProductId,
                            CatalogCode =request.ConfigurationCatalogCode,
                            FootRal =request.FootRal,
                            BodyWingRal =request.BodyWingRal,
                            PlasticColor1No =request.PlasticColor1No,
                            PlasticColor2No =request.PlasticColor2No
                        };
                }

                // UM
                else if (request.ProductGroupId == "2")
                {
                    umConfiguration =
                        new SalesOrderLineUMConfiguration
                        {
                            BodyType =request.BodyType!,
                            BodyCode =request.BodyCode!,
                            IroningBoardCode =request.IroningBoardCode!,
                            FootCode =request.FootCode!,
                            BodyIroningRal =request.BodyIroningRal!,
                            BodyIroningColor =request.BodyIroningColor!,
                            FootRal =request.FootRal!,
                            FootColor =request.FootColor!,
                            FabricCode =request.FabricCode!,
                            FabricName =request.FabricName!,
                            SpongeCode =request.SpongeCode!,
                            SpongeName =request.SpongeName!,
                            SpongeQuantity =request.SpongeQuantity ?? 0,
                            HasFis =request.HasFis,
                            FisType =request.HasFis? request.FisType: null,
                            FisCode =request.HasFis? request.FisCode: null,
                            FisName =request.HasFis? request.FisName: null,
                            PlasticCombinationNo =request.PlasticCombinationNo!,
                            PlasticCombinationDescription =request.PlasticCombinationDescription!
                        };
                }

                // 3. GÖRSELLER
                var imageFiles = new List<(int ImageTypeId, IFormFile File)>();
                if (request.Files != null && request.ImageTypeIds != null)
                {
                    for (int i = 0;i < request.Files.Count;i++)
                    {
                        imageFiles.Add((request.ImageTypeIds[i],request.Files[i]));
                    }
                }

                // 4. COMPLETE CREATE
                var result =await _salesOrderLineService.CreateCompleteAsync(line,ckConfiguration,umConfiguration,imageFiles);

                // 5. SONUÇ KONTROLÜ
                if (result == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message ="Sipariş satırı oluşturulamadı."
                    });
                }

                // 6. RESPONSE
                return Ok(new
                {
                    success = true,
                    id =result.Id,
                    salesOrderId =result.SalesOrderId,
                    lineNumber =result.LineNumber,
                    productGroupId =result.ProductGroupId,
                    productName =result.ProductName,
                    quantity =result.Quantity,
                    status =result.Status,
                    message ="Sipariş satırı başarıyla oluşturuldu."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    innerException =ex.InnerException?.Message,
                    innerInnerException =ex.InnerException?.InnerException?.Message
                });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] SalesOrderLine line)
        {
            try
            {
                var result = await _salesOrderLineService.UpdateAsync(id, line);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Sipariş satırı bulunamadı."
                    });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{lineId:long}")]
        public async Task<IActionResult> Delete(long lineId)
        {
            try
            {
                var result = await _salesOrderLineService.DeleteAsync(lineId);
                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Sipariş satırı bulunamadı veya zaten silinmiş."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Sipariş satırı silindi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{lineId:long}/edit")]
        public async Task<IActionResult> GetForEdit(long lineId)
        {
            try
            {
                var result = await _salesOrderLineService.GetForEditAsync(lineId);
                if (result == null) { return NotFound(new { message = "Sipariş satırı bulunamadı." }); }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{lineId:long}/images/{imageId:long}")]
        public async Task<IActionResult> GetImage(long lineId, long imageId)
        {
            try
            {
                var image = await _salesOrderLineService.GetImageAsync(lineId, imageId);
                if (image == null) { return NotFound(new { message = "Görsel bulunamadı." }); }

                var rootPath = _configuration["FileStorage:RootPath"];
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Dosya depolama RootPath tanımlı değil." });
                }

                var relativePath = image.FilePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(rootPath, relativePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new { message = "Dosya fiziksel olarak bulunamadı." });
                }

                var contentType = GetContentType(image.FileName);
                return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update-complete/{id:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateComplete(long id, [FromForm] SalesOrderLineUpdateModel request)
        {
            try
            {
                if (request.Files != null && request.ImageTypeIds != null && request.Files.Count != request.ImageTypeIds.Count)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Görsel dosyaları ile görsel tipleri eşleşmiyor."
                    });
                }

                var imageFiles = new List<(int ImageTypeId, IFormFile File)>();
                if (request.Files != null && request.ImageTypeIds != null)
                {
                    for (int i = 0; i < request.Files.Count; i++)
                    {
                        imageFiles.Add((request.ImageTypeIds[i], request.Files[i]));
                    }
                }

                var existingImageIds = request.ExistingImageIds ?? new List<long>();
                var result = await _salesOrderLineService.UpdateCompleteAsync(id, request, imageFiles, existingImageIds);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Sipariş satırı bulunamadı." });
                }
                return Ok(new
                {
                    success = true,
                    id = result.Id,
                    salesOrderId = result.SalesOrderId,
                    lineNumber = result.LineNumber,
                    productGroupId = result.ProductGroupId,
                    productName = result.ProductName,
                    quantity = result.Quantity,
                    status = result.Status,
                    message = "Sipariş satırı başarıyla revize edildi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        [HttpGet("product-boxes")]
        public async Task<IActionResult> GetProductBoxes([FromQuery] string urunGrupId,[FromQuery] string katalogKod,[FromQuery] string koliTuru,[FromQuery] string? musteriKod)
        {
            try
            {
                var result = await _salesOrderLineService.GetProductBoxesAsync(urunGrupId,katalogKod,koliTuru,musteriKod);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {success = false,message = ex.Message});
            }
        }
    }
}
