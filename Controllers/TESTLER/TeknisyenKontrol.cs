using GranitWebApi.Data;
using GranitWebApi.Services.XML;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.TESTLER
{
    [ApiController]
    [Route("api/teknisyentest")]
    public class TeknisyenKontrol : ControllerBase
    {
        private readonly IXmlService _xmlService;
        private readonly AppDbContext _db;

        public TeknisyenKontrol(IXmlService xmlService, AppDbContext db)
        {
            _xmlService = xmlService;
            _db = db;
        }

        [HttpGet("run")]
        public async Task<IActionResult> Run()
        {
            try
            {
                var data = await _xmlService.FetchTechnicians();

                foreach (var item in data)
                {
                    var existing = _db.TechnicianLogs.FirstOrDefault(x => x.RN == item.RN);

                    if (existing != null)
                    {
                        existing.MachineName = item.MachineName;
                        existing.TEnd = item.TEnd;
                    }
                    else
                    {
                        _db.TechnicianLogs.Add(item);
                    }
                }

                await _db.SaveChangesAsync();

                return Ok(new { Count = data.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString()); // 👈 KRİTİK
            }
        }
    }
}
