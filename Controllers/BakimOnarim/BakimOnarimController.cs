using Dapper;
using GranitWebApi.Data;
using GranitWebApi.Models.BakimOnarim;
using GranitWebApi.Models.XML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;

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

        [HttpGet("machine-filters")]
        public async Task<IActionResult> GetMachineFilters()
        {
            try
            {
                var machines = await _db.Makine
                    .AsNoTracking().Where(x =>!string.IsNullOrWhiteSpace(x.MachineCode))
                    .Select(x => new
                    {
                        machineCode = x.MachineCode,
                        machineName = x.MachineName,
                        machineGroup = x.Bölüm
                    }).OrderBy(x => x.machineGroup).ThenBy(x => x.machineName).ToListAsync();

                var groups = machines.Select(x => x.machineGroup).Where(x =>!string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct().OrderBy(x => x).ToList();


                return Ok(new
                {
                    groups,
                    machines
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Makine filtreleri alınırken hata oluştu.",
                    detail = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }


        [HttpPost("report")]
        public async Task<IActionResult> GetMaintenanceReport(
    [FromBody] MaintenanceReportRequestDto request)
        {
            if (request == null)
                return BadRequest("Tarih bilgisi gönderilmelidir.");

            if (request.StartDate.Date > request.EndDate.Date)
            {
                return BadRequest(
                    "Başlangıç tarihi bitiş tarihinden büyük olamaz.");
            }

            try
            {
                // =========================================================
                // TARİH ARALIĞI
                // =========================================================

                var startDate = request.StartDate.Date;

                // Bitiş tarihini dahil etmek için bir sonraki günün 00:00'ı
                var endDate = request.EndDate.Date.AddDays(1);


                // =========================================================
                // TEKNİSYEN / PERSONEL LİSTESİ
                // RoleId = 5 olan kullanıcılar
                // =========================================================

                var users = await _db.Database
                    .SqlQuery<TechnicianUserDto>($@"
                SELECT  
                    U.Sicil,  
                    U.FirstName,  
                    U.LastName,  
                    UN.UnitName 
                FROM Users U 

                INNER JOIN PolimekDepartments D
                    ON U.DepartmentId = D.DepartmentId

                INNER JOIN PolimekSubDepartments SD
                    ON U.SubDepartmentId = SD.SubDepartmentId
                    AND U.DepartmentId = SD.DepartmentId

                INNER JOIN PolimekUnits UN
                    ON UN.UnitId = U.UnitId
                    AND U.DepartmentId = UN.DepartmentId
                    AND U.SubDepartmentId = UN.SubDepartmentId

                WHERE U.RoleId = 5
            ")
                    .ToListAsync();


                // =========================================================
                // TAKIM FİLTRESİ
                // =========================================================

                var teamFilter =
                    string.IsNullOrWhiteSpace(request.TeamFilter)
                        ? "all"
                        : request.TeamFilter.Trim().ToLowerInvariant();


                // =========================================================
                // VARDİYA FİLTRESİ
                // =========================================================

                var shiftFilter =
                    string.IsNullOrWhiteSpace(request.ShiftFilter)
                        ? "all"
                        : request.ShiftFilter.Trim();


                // =========================================================
                // MAKİNE GRUBU FİLTRESİ
                // MachineGroup -> Makine.Bölüm
                // =========================================================

                var machineGroupFilter =
                    string.IsNullOrWhiteSpace(request.MachineGroup)
                        ? null
                        : request.MachineGroup.Trim();


                // =========================================================
                // MAKİNE FİLTRESİ
                // MachineCode -> Makine.MachineCode
                // =========================================================

                var machineCodeFilter =
                    string.IsNullOrWhiteSpace(request.MachineCode)
                        ? null
                        : request.MachineCode.Trim();


                // =========================================================
                // TAKIMA GÖRE FİLTRELENMİŞ PERSONELLER
                // =========================================================

                var filteredUsers = users
                    .Where(x => !string.IsNullOrWhiteSpace(x.Sicil))
                    .Where(x =>
                    {
                        var unitName =
                            (x.UnitName ?? "")
                            .Trim()
                            .ToUpperInvariant();

                        return teamFilter switch
                        {
                            "electric" =>
                                unitName.Contains("ELEKTRİK BAKIM"),

                            "mechanical" =>
                                unitName.Contains("MEKANİK BAKIM"),

                            "other" =>
                                !unitName.Contains("ELEKTRİK BAKIM")
                                &&
                                !unitName.Contains("MEKANİK BAKIM"),

                            _ => true
                        };
                    })
                    .ToList();


                // =========================================================
                // FİLTRELENMİŞ TEKNİSYEN SİCİLLERİ
                // =========================================================

                var filteredTechnicianCodes = filteredUsers
                    .Select(x => x.Sicil!)
                    .Distinct()
                    .ToHashSet();


                // =========================================================
                // LOG'LAR
                // =========================================================

                var logs = await _db.TechnicianLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= startDate &&
                        x.CreatedAt.Value < endDate)
                    .ToListAsync();


                // =========================================================
                // MAKİNE GRUBU FİLTRESİ
                //
                // MachineGroup -> Makine.Bölüm
                // =========================================================

                if (!string.IsNullOrWhiteSpace(machineGroupFilter))
                {
                    var machineCodesByGroup = await _db.Database
                        .SqlQuery<string>($@"
                    SELECT MachineCode
                    FROM Makine
                    WHERE Bölüm = {machineGroupFilter}
                ")
                        .ToListAsync();

                    var machineCodeSet = machineCodesByGroup
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToHashSet();

                    logs = logs
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.MachineCode) &&
                            machineCodeSet.Contains(x.MachineCode!))
                        .ToList();
                }


                // =========================================================
                // TEK MAKİNE FİLTRESİ
                // =========================================================

                if (!string.IsNullOrWhiteSpace(machineCodeFilter))
                {
                    logs = logs
                        .Where(x =>
                            x.MachineCode == machineCodeFilter)
                        .ToList();
                }


                // =========================================================
                // TAKIM FİLTRESİ
                //
                // TechnicianCode -> Users.Sicil
                // =========================================================

                if (teamFilter != "all")
                {
                    logs = logs
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.TechnicianCode)
                            &&
                            filteredTechnicianCodes.Contains(
                                x.TechnicianCode!))
                        .ToList();
                }


                // =========================================================
                // VARDİYA FİLTRESİ
                // =========================================================

                if (shiftFilter != "all")
                {
                    logs = logs
                        .Where(x =>
                            x.Shift != null &&
                            x.Shift.ToString() == shiftFilter)
                        .ToList();
                }


                // =========================================================
                // GENEL KPI
                // =========================================================

                var totalCalls = logs.Count;

                var openCalls =
                    logs.Count(IsOpen);

                var operatorClosedCalls =
                    logs.Count(IsOperatorClosed);

                var inProgressCalls =
                    logs.Count(IsInProgress);


                var completedLogs = logs
                    .Where(IsCompleted)
                    .ToList();

                var completedCalls =
                    completedLogs.Count;

                var detailedCompletedCalls =
                    completedLogs.Count(x => x.IsDetailed);

                var undetailedCompletedCalls =
                    completedLogs.Count(x => !x.IsDetailed);


                // =========================================================
                // ORTALAMA REAKSİYON SÜRESİ
                // TLogin - TCall
                // =========================================================

                var reactionTimes = logs
                    .Where(x =>
                        !IsEmptyDate(x.TCall) &&
                        !IsEmptyDate(x.TLogin))
                    .Select(x =>
                        (x.TLogin!.Value - x.TCall!.Value)
                        .TotalMinutes)
                    .Where(x => x >= 0)
                    .ToList();

                var averageReactionMinutes =
                    reactionTimes.Any()
                        ? reactionTimes.Average()
                        : 0;


                // =========================================================
                // ORTALAMA MÜDAHALE SÜRESİ
                // TEnd - TLogin
                // =========================================================

                var interventionTimes = logs
                    .Where(x =>
                        !IsEmptyDate(x.TLogin) &&
                        !IsEmptyDate(x.TEnd))
                    .Select(x =>
                        (x.TEnd!.Value - x.TLogin!.Value)
                        .TotalMinutes)
                    .Where(x => x >= 0)
                    .ToList();

                var averageInterventionMinutes =
                    interventionTimes.Any()
                        ? interventionTimes.Average()
                        : 0;


                // =========================================================
                // GÜNLÜK TREND
                // =========================================================

                var dailyTrend = logs
                    .Where(x => x.CreatedAt.HasValue)
                    .GroupBy(x => x.CreatedAt!.Value.Date)
                    .OrderBy(x => x.Key)
                    .Select(g =>
                    {
                        var dayLogs = g.ToList();

                        var dayCompleted =
                            dayLogs
                                .Where(IsCompleted)
                                .ToList();

                        var dayReactionTimes = dayLogs
                            .Where(x =>
                                !IsEmptyDate(x.TCall) &&
                                !IsEmptyDate(x.TLogin))
                            .Select(x =>
                                (x.TLogin!.Value - x.TCall!.Value)
                                .TotalMinutes)
                            .Where(x => x >= 0)
                            .ToList();

                        var dayInterventionTimes = dayLogs
                            .Where(x =>
                                !IsEmptyDate(x.TLogin) &&
                                !IsEmptyDate(x.TEnd))
                            .Select(x =>
                                (x.TEnd!.Value - x.TLogin!.Value)
                                .TotalMinutes)
                            .Where(x => x >= 0)
                            .ToList();

                        return new MaintenanceDailyTrendDto
                        {
                            Date = g.Key,

                            TotalCalls =
                                dayLogs.Count,

                            OpenCalls =
                                dayLogs.Count(IsOpen),

                            InProgressCalls =
                                dayLogs.Count(IsInProgress),

                            OperatorClosedCalls =
                                dayLogs.Count(IsOperatorClosed),

                            CompletedCalls =
                                dayCompleted.Count,

                            DetailedCompletedCalls =
                                dayCompleted.Count(x => x.IsDetailed),

                            UndetailedCompletedCalls =
                                dayCompleted.Count(x => !x.IsDetailed),

                            AverageReactionMinutes =
                                dayReactionTimes.Any()
                                    ? Math.Round(
                                        dayReactionTimes.Average(),
                                        2)
                                    : 0,

                            AverageInterventionMinutes =
                                dayInterventionTimes.Any()
                                    ? Math.Round(
                                        dayInterventionTimes.Average(),
                                        2)
                                    : 0
                        };
                    })
                    .ToList();


                // =========================================================
                // TEKNİSYEN RAPORU
                // =========================================================

                var technicianCodes = logs
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.TechnicianCode))
                    .Select(x => x.TechnicianCode!)
                    .Distinct()
                    .ToList();

                var technicianUsers = users
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Sicil)
                        &&
                        technicianCodes.Contains(x.Sicil!))
                    .ToList();

                var technicianReports = logs
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.TechnicianCode))
                    .GroupBy(x => x.TechnicianCode)
                    .Select(g =>
                    {
                        var technicianLogs = g.ToList();

                        var technicianCode = g.Key;

                        var user =
                            technicianUsers.FirstOrDefault(x =>
                                x.Sicil == technicianCode);


                        var technicianReactionTimes =
                            technicianLogs
                                .Where(x =>
                                    !IsEmptyDate(x.TCall) &&
                                    !IsEmptyDate(x.TLogin))
                                .Select(x =>
                                    (x.TLogin!.Value -
                                     x.TCall!.Value)
                                    .TotalMinutes)
                                .Where(x => x >= 0)
                                .ToList();


                        var technicianInterventionTimes =
                            technicianLogs
                                .Where(x =>
                                    !IsEmptyDate(x.TLogin) &&
                                    !IsEmptyDate(x.TEnd))
                                .Select(x =>
                                    (x.TEnd!.Value -
                                     x.TLogin!.Value)
                                    .TotalMinutes)
                                .Where(x => x >= 0)
                                .ToList();


                        var technicianName =
                            user != null
                                ? $"{user.FirstName} {user.LastName}".Trim()
                                : technicianLogs
                                    .Select(x => x.TechnicianName)
                                    .FirstOrDefault(x =>
                                        !string.IsNullOrWhiteSpace(x))
                                    ?? "Bilinmeyen Teknisyen";


                        return new MaintenanceTechnicianReportDto
                        {
                            TechnicianCode =
                                technicianCode ?? "",

                            TechnicianName =
                                technicianName,

                            CallCount =
                                technicianLogs.Count,

                            CompletedCount =
                                technicianLogs.Count(IsCompleted),

                            OpenCount =
                                technicianLogs.Count(IsOpen),

                            InProgressCount =
                                technicianLogs.Count(IsInProgress),

                            OperatorClosedCount =
                                technicianLogs.Count(IsOperatorClosed),

                            AverageReactionMinutes =
                                technicianReactionTimes.Any()
                                    ? Math.Round(
                                        technicianReactionTimes.Average(),
                                        2)
                                    : 0,

                            AverageInterventionMinutes =
                                technicianInterventionTimes.Any()
                                    ? Math.Round(
                                        technicianInterventionTimes.Average(),
                                        2)
                                    : 0,

                            DetailedCount =
                                technicianLogs.Count(x =>
                                    IsCompleted(x)
                                    && x.IsDetailed),

                            UndetailedCount =
                                technicianLogs.Count(x =>
                                    IsCompleted(x)
                                    && !x.IsDetailed)
                        };
                    })
                    .OrderByDescending(x => x.CallCount)
                    .ToList();


                // =========================================================
                // MAKİNE RAPORU
                // =========================================================

                var machineReports = logs
                    .GroupBy(x => new
                    {
                        x.MachineCode,
                        x.MachineName
                    })
                    .Select(g =>
                    {
                        var machineLogs = g.ToList();

                        var machineReactionTimes =
                            machineLogs
                                .Where(x =>
                                    !IsEmptyDate(x.TCall) &&
                                    !IsEmptyDate(x.TLogin))
                                .Select(x =>
                                    (x.TLogin!.Value -
                                     x.TCall!.Value)
                                    .TotalMinutes)
                                .Where(x => x >= 0)
                                .ToList();

                        var machineInterventionTimes =
                            machineLogs
                                .Where(x =>
                                    !IsEmptyDate(x.TLogin) &&
                                    !IsEmptyDate(x.TEnd))
                                .Select(x =>
                                    (x.TEnd!.Value -
                                     x.TLogin!.Value)
                                    .TotalMinutes)
                                .Where(x => x >= 0)
                                .ToList();

                        var machineCompleted =
                            machineLogs
                                .Where(IsCompleted)
                                .ToList();

                        return new MaintenanceMachineReportDto
                        {
                            MachineCode =
                                g.Key.MachineCode ?? "",

                            MachineName =
                                g.Key.MachineName ?? "",

                            CallCount =
                                machineLogs.Count,

                            OpenCalls =
                                machineLogs.Count(IsOpen),

                            InProgressCalls =
                                machineLogs.Count(IsInProgress),

                            CompletedCalls =
                                machineCompleted.Count,

                            OperatorClosedCalls =
                                machineLogs.Count(IsOperatorClosed),

                            DetailedCompletedCalls =
                                machineCompleted.Count(x => x.IsDetailed),

                            UndetailedCompletedCalls =
                                machineCompleted.Count(x => !x.IsDetailed),

                            AverageReactionMinutes =
                                machineReactionTimes.Any()
                                    ? Math.Round(
                                        machineReactionTimes.Average(),
                                        2)
                                    : 0,

                            AverageInterventionMinutes =
                                machineInterventionTimes.Any()
                                    ? Math.Round(
                                        machineInterventionTimes.Average(),
                                        2)
                                    : 0
                        };
                    })
                    .OrderByDescending(x => x.CallCount)
                    .ToList();


                // =========================================================
                // SONUÇ
                // =========================================================

                var result = new MaintenanceReportDto
                {
                    StartDate =
                        request.StartDate.Date,

                    EndDate =
                        request.EndDate.Date,

                    TotalCalls =
                        totalCalls,

                    OpenCalls =
                        openCalls,

                    InProgressCalls =
                        inProgressCalls,

                    CompletedCalls =
                        completedCalls,

                    OperatorClosedCalls =
                        operatorClosedCalls,

                    DetailedCompletedCalls =
                        detailedCompletedCalls,

                    UndetailedCompletedCalls =
                        undetailedCompletedCalls,

                    AverageReactionMinutes =
                        Math.Round(
                            averageReactionMinutes,
                            2),

                    AverageInterventionMinutes =
                        Math.Round(
                            averageInterventionMinutes,
                            2),

                    DailyTrend =
                        dailyTrend,

                    Technicians =
                        technicianReports,

                    Machines =
                        machineReports
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        private static bool IsEmptyDate(DateTime? date)
        {
            return !date.HasValue ||
                   date.Value == DateTime.MinValue ||
                   date.Value.Year <= 1;
        }
        private static bool IsOpen(TechnicianModel x)
        {
            return IsEmptyDate(x.TLogin) &&
                   IsEmptyDate(x.TEnd);
        }
        private static bool IsOperatorClosed(TechnicianModel x)
        {
            return IsEmptyDate(x.TLogin) &&
                   !IsEmptyDate(x.TEnd);
        }
        private static bool IsInProgress(TechnicianModel x)
        {
            return !IsEmptyDate(x.TLogin) &&
                   IsEmptyDate(x.TEnd);
        }
        private static bool IsCompleted(TechnicianModel x)
        {
            return !IsEmptyDate(x.TLogin) &&
                   !IsEmptyDate(x.TEnd);
        }
        string GetTeam(string? unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return "other";

            var name = unitName.ToUpperInvariant();

            if (name.Contains("ELEKTRİK"))
                return "electric";

            if (name.Contains("MEKANİK"))
                return "mechanical";

            return "other";
        }
    }
}