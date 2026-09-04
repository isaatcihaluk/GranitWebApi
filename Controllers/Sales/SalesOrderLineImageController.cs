using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Sales
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderLineImageController : ControllerBase
    {
        private readonly ISalesOrderLineImageService _imageService;

        public SalesOrderLineImageController(ISalesOrderLineImageService imageService)
        {
            _imageService = imageService;
        }

        // GET: api/SalesOrderLineImage/line/1
        [HttpGet("line/{salesOrderLineId:long}")]
        public async Task<IActionResult> GetBySalesOrderLineId(long salesOrderLineId)
        {
            try
            {
                var images =await _imageService.GetBySalesOrderLineIdAsync(salesOrderLineId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // =========================================================
        // GET: api/SalesOrderLineImage/1
        // =========================================================

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var image =await _imageService.GetByIdAsync(id);

                if (image == null)
                {
                    return NotFound(new
                    {
                        message = "Görsel bulunamadı."
                    });
                }

                return Ok(image);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // =========================================================
        // POST: api/SalesOrderLineImage
        // DOSYA YÜKLEME
        // =========================================================

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] long salesOrderLineId,[FromForm] int imageTypeId,IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        message = "Lütfen bir dosya seçiniz."
                    });
                }

                // Şimdilik test amaçlı.
                // JWT kullanıcı yapısını daha sonra bağlayacağız.
                var createdBy = 1;

                var result =await _imageService.CreateAsync(salesOrderLineId,imageTypeId,file,createdBy);
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

        // =========================================================
        // PUT: api/SalesOrderLineImage/1
        // =========================================================

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id,[FromBody] SalesOrderLineImage image)
        {
            try
            {
                var result =await _imageService.UpdateAsync(id,image);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Görsel bulunamadı."
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

        // DELETE: api/SalesOrderLineImage/1

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result =await _imageService.DeleteAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Görsel bulunamadı."
                    });
                }

                return Ok(new
                {
                    message = "Görsel pasif duruma getirildi."
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
