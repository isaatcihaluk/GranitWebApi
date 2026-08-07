using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Workflow.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Workflow.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowQueryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserContext _userContext;

        public WorkflowQueryController(AppDbContext context, UserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }
        /// (Taslak + Revize + Aktif Onay)
        [HttpGet("active")]
        public async Task<IActionResult> Active()
        {
            var userId = _userContext.UserId;
            try
            {
                var data = await _context.ProcessRequest
                    .AsNoTracking()
                    .Where(x =>
                        (x.CreatedBy == userId &&
                         (x.Status == WorkflowStatus.Taslak.ToString()
                          || x.Status == WorkflowStatus.Revize.ToString()))
                        ||
                        x.Approvals.Any(a =>
                            a.ApproverId == userId &&
                            a.IsActive))
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => new
                    {
                        x.Id,
                        x.ProcessTypeId,
                        x.Status,
                        x.EntityId,
                        x.CurrentStep,
                        x.CreatedDate,
                        x.CreatedBy,
                        x.IsCompleted
                    }).ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }

        /// Oluşturduğum fakat şu anda başka kullanıcıda bekleyen süreçler        
        [HttpGet("my")]
        public async Task<IActionResult> My()
        {
            var userId = _userContext.UserId;
            var data = await _context.ProcessRequest
                .AsNoTracking()
                .Where(x =>
                    x.CreatedBy == userId &&
                    x.Status == WorkflowStatus.Onayda.ToString() &&
                    !x.Approvals.Any(a =>
                        a.ApproverId == userId &&
                        a.IsActive))
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    x.Id,
                    x.ProcessTypeId,
                    x.Status,
                    x.EntityId,
                    x.CurrentStep,
                    x.CreatedDate,
                    x.CreatedBy,
                    x.IsCompleted
                })
                .ToListAsync();

            return Ok(data);
        }

        /// Tamamlanmış süreçler
        [HttpGet("history")]
        public async Task<IActionResult> History()
        {
            var userId = _userContext.UserId;

            var data = await _context.ProcessRequest
                .AsNoTracking()
                .Where(x =>
                    x.IsCompleted &&
                    (
                        x.CreatedBy == userId ||
                        x.Histories.Any(h => h.UserId == userId)
                    ))
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    x.Id,
                    x.ProcessTypeId,
                    x.Status,
                    x.EntityId,
                    x.CurrentStep,
                    x.CreatedDate,
                    x.CreatedBy,

                    IzinTuruId = _context.IzinTalepleri
                        .Where(i => i.Id == x.EntityId)
                        .Select(i => i.IzinTuruId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(data);
        }

        /// Süreç Detayı
        [HttpGet("detail/{requestId}")]
        public async Task<IActionResult> Detail(int requestId)
        {
            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.ProcessType)
                    .Include(x => x.Approvals)
                        .ThenInclude(x => x.WorkflowStep)
                    .Include(x => x.Histories)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null)
                    return NotFound();

                var userIds = request.Approvals
                    .Select(x => x.ApproverId)
                    .Union(
                        request.Histories.Select(x => x.UserId)
                    )
                    .Append(request.CreatedBy)
                    .Distinct()
                    .ToList();

                var userList = await _context.Users.ToListAsync();

                var users = userList
                    .Where(x => userIds.Contains(x.Id))
                    .ToDictionary(
                        x => x.Id,
                        x => $"{x.FirstName} {x.LastName}"
                    );

                return Ok(new
                {
                    request.Id,
                    request.ProcessTypeId,
                    request.CreatedBy,
                    ProcessName = request.ProcessType != null ? request.ProcessType.Name : "",
                    CreatedByName = users.ContainsKey(request.CreatedBy) ? users[request.CreatedBy] : "",

                    request.Title,
                    request.Status,
                    request.CreatedDate,
                    request.CurrentStep,
                    request.IsCompleted,


                    Approvals = request.Approvals
                        .Where(a => a.Status != WorkflowStatus.Iptal.ToString())
                        .OrderBy(x => x.StepOrder)
                        .Select(a => new
                        {
                            a.Id,

                            StepDescription = a.WorkflowStep != null
                                ? a.WorkflowStep.Description
                                : "",

                            a.ApproverId,

                            ApproverName = users.ContainsKey(a.ApproverId)
                                ? users[a.ApproverId]
                                : "",

                            a.WorkflowStepId,
                            a.StepOrder,
                            a.Status,
                            a.ActionDate,
                            a.Comment,
                            a.IsActive
                        }),

                    Histories = request.Histories
                        .OrderBy(x => x.ActionDate)
                        .Select(h => new
                        {
                            h.Id,
                            h.UserId,

                            UserName = users.ContainsKey(h.UserId)
                                ? users[h.UserId]
                                : "",

                            ActionName = ((WorkflowAction)h.ActionType) switch
                            {
                                WorkflowAction.Baslat => "Talep Oluşturuldu",
                                WorkflowAction.Gonder => "Onaya Gönderildi",
                                WorkflowAction.Onayla => "Onaylandı",
                                WorkflowAction.Reddet => "Reddedildi",
                                WorkflowAction.Revize => "Revize İstendi",
                                WorkflowAction.Iptal => "İptal Edildi",
                                WorkflowAction.Tamamla => "Tamamlandı",
                                WorkflowAction.TekrarGonder => "Tekrar Onaya Gönderildi",
                                _ => ""
                            },
                            h.ActionDate,
                            h.Comment
                        })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }
    }
}