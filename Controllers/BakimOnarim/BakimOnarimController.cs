using Dapper;
using GranitWebApi.Data;
using GranitWebApi.Models.BakimOnarim;
using GranitWebApi.Models.XML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Controllers.BakımOnarım
{
    [ApiController]
    [Route("api/bakimonarim")]
    [Authorize]
    public class BakimOnarimController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public BakimOnarimController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // 📄 LIST
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var data = await _db.TechnicianLogs
                    .OrderBy(x => x.RN)
                    .OrderBy(x => x.Status)
                    .Select(x => new
                    {
                        x.RN,
                        OperatorName = x.OperatorName ?? "-",
                        MachineName = x.MachineName ?? "-",
                        TechnicianName = x.TechnicianName ?? "-",

                        TCall = x.TCall,
                        TLogin = x.TLogin,
                        TEnd = x.TEnd,

                        Duration = x.Duration.ToString(),         // 🔥 FIX
                        ReactionTime = x.ReactionTime.ToString(), // 🔥 FIX

                        x.Shift,
                        x.IsDetailed,

                        Status = x.Status ?? "-"
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); // 🔥 gerçek hatayı gör
            }
        }

        // 📄 DETAIL
        [HttpGet("detail/{rn}")]
        public async Task<IActionResult> GetDetail(int rn)
        {
            var data = await _db.TechnicianLogs
                .Where(x => x.RN == rn)
                .Select(x => new
                {
                    x.RN,
                    x.MachineName,
                    x.OperatorName,
                    x.TechnicianName,
                    x.TCall,
                    x.TLogin,
                    x.TEnd,
                    x.Duration,
                    x.ReactionTime,
                    x.Shift,
                    x.IsDetailed,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (data == null)
                return NotFound("Log bulunamadı");

            return Ok(data);
        }

        // 🔧 ISSUE TYPES
        [HttpGet("issue-main")]
        public IActionResult GetMainIssues()
        {
            var data = _db.TechnicianMaintenanceIssueTypes
                .Where(x => x.ParentId == null && x.IsActive)
                .Select(x => new { id = x.Id, name = x.Name })
                .ToList();

            return Ok(data);
        }

        [HttpGet("issue-sub/{parentId}")]
        public IActionResult GetSubIssues(int parentId)
        {
            var data = _db.TechnicianMaintenanceIssueTypes
                .Where(x => x.ParentId == parentId && x.IsActive)
                .Select(x => new { id = x.Id, name = x.Name })
                .ToList();

            return Ok(data);
        }

        // 🔧 ACTION TYPES
        [HttpGet("action-types")]
        public async Task<IActionResult> GetActionTypes()
        {
            var data = await _db.TechnicianMaintenanceActionTypes
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                })
                .ToListAsync();

            return Ok(data);
        }

        // 🔧 ERP PARTS
        [HttpGet("parts")]
        public async Task<IActionResult> GetParts()
        {
            using var conn = new SqlConnection(
                _config.GetConnectionString("ERPConnection"));

            var sql = @"
                SELECT 
                    ISNULL(STOK_KODU,'') AS PartCode,
                    ISNULL(STOK_ADI,'') AS PartName,
                    1 AS Quantity
                FROM GRANITMTL2026..TBLSTSABIT
                WHERE STOK_KODU LIKE '46%'
            ";

            var data = await conn.QueryAsync<PartDto>(sql);

            return Ok(data);
        }

        // 💾 SAVE ALL (FINAL)
        [HttpPost("save-all")]
        public async Task<IActionResult> SaveAll([FromBody] SaveAllDto dto)
        {
            if (dto == null) return BadRequest("Boş veri");
            if (dto.Issue == null || dto.Issue.IssueTypeId == 0) return BadRequest("Arıza seçilmelidir");
            if (dto.Action == null || dto.Action.ActionTypeId == 0) return BadRequest("İşlem seçilmelidir");
            using var tran = await _db.Database.BeginTransactionAsync();

            try
            {
                // =====================
                // ISSUE
                // =====================
                var issue = new TechnicianMaintenanceIssue
                {
                    LogRN = dto.LogRN,
                    IssueTypeId = dto.Issue.IssueTypeId,
                    Description = dto.Issue.Description ?? "-",
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "system",
                    ComponentId = dto.Issue.ComponentId
                };
                _db.TechnicianMaintenanceIssues.Add(issue);
                await _db.SaveChangesAsync();
                // =====================
                // ACTION
                // =====================

                var action = new TechnicianMaintenanceAction
                {
                    LogRN = dto.LogRN,
                    ActionTypeId = dto.Action.ActionTypeId,
                    Description = string.IsNullOrWhiteSpace(dto.Action.Description)
                        ? "-"
                        : dto.Action.Description,
                    SolutionType = 1,
                    ActionTime = DateTime.Now
                };

                _db.TechnicianMaintenanceActions.Add(action);
                await _db.SaveChangesAsync();

                if (action.Id <= 0) throw new Exception("Action insert başarısız.");
                // =====================
                // PARTS
                // =====================
                if (dto.Parts != null && dto.Parts.Any())
                {
                    foreach (var p in dto.Parts)
                    {
                        _db.TechnicianMaintenanceParts.Add(new TechnicianMaintenancePart
                        {
                            ActionId = action.Id,
                            LogRN = dto.LogRN,
                            PartCode = p.PartCode,
                            PartName = p.PartName,
                            Quantity = p.Quantity > 0 ? p.Quantity : 1
                        });
                    }

                    await _db.SaveChangesAsync();
                }
                // =====================
                // YARDIMCI TEKNİSYENLER
                // =====================
                var oldTechnicians = await _db.TechnicianLogTechnicians
                    .Where(x => x.LogRN == dto.LogRN)
                    .ToListAsync();

                if (oldTechnicians.Any())
                    _db.TechnicianLogTechnicians.RemoveRange(oldTechnicians);

                if (dto.TechnicianIds != null && dto.TechnicianIds.Any())
                {
                    foreach (var techId in dto.TechnicianIds.Distinct())
                    {
                        _db.TechnicianLogTechnicians.Add(new TechnicianLogTechnicians
                        {
                            LogRN = dto.LogRN,
                            UserId = techId,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                await _db.SaveChangesAsync();

                // LOG UPDATE
                var log = await _db.TechnicianLogs
                    .FirstOrDefaultAsync(x => x.RN == dto.LogRN);

                if (log == null) return BadRequest("Log bulunamadı");

                log.IsDetailed = true;
                log.Status = "Tamamlandı";
                await _db.SaveChangesAsync();
                await tran.CommitAsync();
                return Ok(new
                {
                    success = true,
                    issueId = issue.Id,
                    actionId = action.Id
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("detailview/{rn}")]
        public async Task<IActionResult> GetDetailVies(int rn)
        {
            var log = await _db.TechnicianLogs
                .Where(x => x.RN == rn)
                .Select(x => new
                {
                    x.RN,
                    x.MachineCode,
                    x.MachineName,
                    x.OperatorName,
                    x.TechnicianName,
                    x.TCall,
                    x.TLogin,
                    x.TEnd,
                    x.Duration,
                    x.ReactionTime,
                    x.Shift,
                    x.IsDetailed,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (log == null)
                return NotFound();

            var issue = await _db.TechnicianMaintenanceIssues
                .Where(x => x.LogRN == rn)
                .Select(x => new
                {
                    issueTypeId = x.IssueTypeId,
                    description = x.Description,
                    parentId = x.IssueType.ParentId,
                    componentId = x.ComponentId
                })
                .FirstOrDefaultAsync();

            var action = await _db.TechnicianMaintenanceActions
                .Where(x => x.LogRN == rn)
                .Select(x => new
                {
                    actionTypeId = x.ActionTypeId,
                    description = x.Description,
                    solutionType = x.SolutionType
                })
                .FirstOrDefaultAsync();

            var actionId = await _db.TechnicianMaintenanceActions
                .Where(x => x.LogRN == rn)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var parts = await _db.TechnicianMaintenanceParts
                .Where(x => x.ActionId == actionId)
                .Select(x => new
                {
                    partCode = x.PartCode,
                    partName = x.PartName,
                    quantity = x.Quantity
                })
                .ToListAsync();
            var technicians = await _db.TechnicianLogTechnicians
                .Where(x => x.LogRN == rn)
                .Select(x => x.UserId)
                .ToListAsync();

            return Ok(new
            {
                log,
                issue,
                action,
                parts,
                technicians
            });
        }

        // ➕ MANUEL KAYIT
        [HttpPost("manual-create")]
        public async Task<IActionResult> ManualCreate([FromBody] ManualMaintenanceCreateDto dto)
        {
            using var tran = await _db.Database.BeginTransactionAsync();
            try
            {
                // Son RN
                var firstTechnicianId = dto.TechnicianIds.FirstOrDefault();

                const int manualRnStart = 900000000;
                var lastRn = await _db.TechnicianLogs.Where(x => x.RN >= manualRnStart).MaxAsync(x => (int?)x.RN) ?? manualRnStart;
                var newRn = lastRn + 1;

                var entity = new TechnicianModel
                {
                    RN = newRn,
                    MachineCode = dto.MachineCode,
                    MachineName = dto.MachineName,
                    Operator = dto.Operator ?? 0,
                    OperatorCode = dto.OperatorCode,
                    OperatorName = dto.OperatorName,
                    // İlk teknisyeni ana kayıtta da tut
                    Technician = firstTechnicianId == 0 ? null : firstTechnicianId,
                    TechnicianCode = dto.TechnicianCode,
                    TechnicianName = dto.TechnicianName,

                    TCall = dto.TCall,
                    TLogin = dto.TLogin,
                    TEnd = dto.TEnd,
                    Shift = dto.Shift,
                    IsDetailed = false,
                    Status = "Açık"
                };

                _db.TechnicianLogs.Add(entity);
                await _db.SaveChangesAsync();

                // Seçilen teknisyenleri ilişki tablosuna kaydet
                if (dto.TechnicianIds != null && dto.TechnicianIds.Any())
                {
                    foreach (var technicianId in dto.TechnicianIds.Distinct())
                    {
                        _db.TechnicianLogTechnicians.Add(new TechnicianLogTechnicians
                        {
                            LogRN = newRn,
                            UserId = technicianId,
                            CreatedAt = DateTime.Now
                        });
                    }

                    await _db.SaveChangesAsync();
                }
                await tran.CommitAsync();
                return Ok(new
                {
                    success = true,
                    rn = newRn
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return StatusCode(500, ex.ToString());
            }
        }

    }
}