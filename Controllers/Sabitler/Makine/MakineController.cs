using GranitWebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Controllers.Sabitler.Makineler
{
    [ApiController]
    [Route("api/makine")]
    [Authorize]
    public class MakineController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public MakineController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var data = await _db.Makine
                    .OrderByDescending(x => x.MachineCode)
                    .Select(x => new
                    {
                       x.MachineCode,
                       x.MachineName
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); // 🔥 gerçek hatayı gör
            }
        }

        [HttpGet("{machineCode}/components")]
        public async Task<IActionResult> GetMachineComponents(string machineCode)
        {
            try
            {
                var data = await _db.MakineBilesenler
                    .Where(x => x.ComponentCode.StartsWith(machineCode))
                    .OrderBy(x => x.ComponentName)
                    .Select(x => new
                    {
                        x.ComponentId,
                        x.ComponentCode,
                        x.ComponentName
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
