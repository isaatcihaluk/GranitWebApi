using GranitWebApi.Data;
using GranitWebApi.Models.IK.Izin;
using GranitWebApi.Services.Notifications;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Workflow.Services
{
    public class WorkflowService : IWorkflowService
    {
        #region Constructor

        private readonly AppDbContext _context;
        private readonly IEnumerable<IApprovalResolver> _resolvers;
        private readonly WorkflowNotificationService _workflowNotificationService;

        public WorkflowService(
            AppDbContext context,
            IEnumerable<IApprovalResolver> resolvers,
            WorkflowNotificationService workflowNotificationService)
        {
            _context = context;
            _resolvers = resolvers;
            _workflowNotificationService = workflowNotificationService;
        }

        #endregion

        #region Public Methods

        public async Task<int> StartAsync(ProcessRequest request)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                request.Status = WorkflowStatus.Taslak.ToString();
                request.CreatedDate = DateTime.Now;
                request.IsCompleted = false;
                request.CurrentStep = 0;


                if (request.Histories == null)
                    request.Histories = new List<WorkflowHistory>();


                _context.ProcessRequest.Add(request);

                await _context.SaveChangesAsync();


                AddHistory(
                    request,
                    request.CreatedBy,
                    WorkflowAction.Baslat,
                    "Talep oluşturuldu."
                );


                await _context.SaveChangesAsync();


                await transaction.CommitAsync();


                return request.Id;

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task SubmitAsync(int requestId, int userId, string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null)
                    throw new Exception("Talep bulunamadı.");

                if (request.CreatedBy != userId)
                    throw new Exception("Sadece talep sahibi gönderebilir.");

                if (request.Status != WorkflowStatus.Taslak.ToString())
                    throw new Exception("Sadece taslak durumundaki talepler gönderilebilir.");

                var workflow = await GetWorkflowDefinitionAsync(request.ProcessTypeId);

                var approvals = await BuildApprovalsAsync(workflow, request);

                if (!approvals.Any())
                    throw new Exception("Workflow için onaylayıcı bulunamadı.");

                // İlk onay adımını aktif et
                ActivateFirstStep(approvals);

                request.Status = WorkflowStatus.Onayda.ToString();
                request.CurrentStep = approvals.Min(x => x.StepOrder);
                request.IsCompleted = false;

                _context.ProcessApproval.AddRange(approvals);

                AddHistory(
                    request,
                    userId,
                    WorkflowAction.Gonder,
                    comment ?? "Talep onaya gönderildi."
                );

                await _context.SaveChangesAsync();

                // Önce veritabanını kesinleştir
                await transaction.CommitAsync();

                // Mail başarısız olsa bile workflow geri alınmasın
                try
                {
                    await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                }
                catch (Exception ex)
                {
                    // Şimdilik logla
                    Console.WriteLine($"Mail gönderilemedi: {ex.Message}");

                    // İleride NotificationLog tablosuna yazacağız.
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task ApproveAsync(int requestId,int userId,string? comment)
        {
            using var transaction =await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null)
                    throw new Exception("Talep bulunamadı.");

                if (request.IsCompleted)
                    throw new Exception(
                        "Bu süreç zaten tamamlanmış.");

                if (request.Status != WorkflowStatus.Onayda.ToString())
                    throw new Exception(
                        "Bu süreç onay durumunda değil.");

                var approval = request.Approvals
                    .FirstOrDefault(x =>
                        x.ApproverId == userId &&
                        x.IsActive);

                if (approval == null)
                    throw new Exception(
                        "Bu kullanıcının aktif onayı bulunmamaktadır.");

                var workflowStep =
                    await _context.WorkflowStep
                    .FirstOrDefaultAsync(x =>
                        x.Id == approval.WorkflowStepId);

                if (workflowStep == null)
                    throw new Exception("Workflow adımı bulunamadı.");

                // Aktif onayı tamamla
                approval.Status =WorkflowStatus.Onaylandi.ToString();
                approval.ActionDate =DateTime.Now;
                approval.Comment =comment;
                approval.IsActive =false;



                switch (workflowStep.StepType)
                {
                    case WorkflowStepType.Any:

                        foreach (var item in request.Approvals
                            .Where(x =>
                                x.WorkflowStepId == approval.WorkflowStepId &&
                                x.Status == WorkflowStatus.Onayda.ToString()))
                        {
                            item.IsActive = false;
                            item.Status = WorkflowStatus.Iptal.ToString();
                        }

                        ActivateNextStep(
                            request,
                            approval.StepOrder);

                        break;


                    case WorkflowStepType.Parallel:

                        var pendingParallel =
                            request.Approvals.Any(x =>
                                x.WorkflowStepId == approval.WorkflowStepId &&
                                x.Status == WorkflowStatus.Onayda.ToString());

                        if (!pendingParallel)
                        {
                            ActivateNextStep(
                                request,
                                approval.StepOrder);
                        }

                        break;


                    default:

                        ActivateNextStep(
                            request,
                            approval.StepOrder);

                        break;
                }
                if (!request.IsCompleted)
                {
                    await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                }

                if (request.IsCompleted)
                {
                    var izin = await _context.IzinTalepleri
                        .FirstOrDefaultAsync(x => x.ProcessRequestId == request.Id);

                    if (izin != null)
                    {
                        izin.Durum = WorkflowStatus.Tamamlandi.ToString();
                        izin.GuncellemeTarihi = DateTime.Now;

                        await IzinBakiyesiniDusAsync(izin);
                    }
                }
                if (request.IsCompleted)
                {
                    await _workflowNotificationService.SendWorkflowCompletedAsync(request.Id);
                }

                AddHistory(request,userId,WorkflowAction.Onayla,comment ?? "Talep onaylandı.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task RejectAsync(int requestId,int userId,string? comment)
        {
            using var transaction =await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals)
                    .FirstOrDefaultAsync(x => x.Id == requestId);


                if (request == null)
                    throw new Exception("Talep bulunamadı.");


                if (request.IsCompleted)
                    throw new Exception("Tamamlanmış süreç reddedilemez.");

                var approval = request.Approvals
                    .FirstOrDefault(x =>
                        x.ApproverId == userId &&
                        x.IsActive);

                if (approval == null)
                    throw new Exception("Aktif onay bulunamadı.");

                approval.Status = WorkflowStatus.Reddedildi.ToString();
                approval.ActionDate = DateTime.Now;
                approval.Comment = comment;
                approval.IsActive = false;

                foreach (var item in request.Approvals
                    .Where(x =>x.Status == WorkflowStatus.Onayda.ToString()&& x.IsActive))
                {
                    item.IsActive = false;
                    item.Status =WorkflowStatus.Iptal.ToString();
                    item.ActionDate = DateTime.Now;
                }

                request.Status =WorkflowStatus.Reddedildi.ToString();
                request.IsCompleted = true;
                request.CurrentStep = 0;

                AddHistory(request,userId,WorkflowAction.Reddet,comment);

                await _context.SaveChangesAsync();
                await _workflowNotificationService.SendWorkflowRejectedAsync(request.Id,comment);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task ReturnAsync(int requestId,int userId,string? comment)
        {
            using var transaction =await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ProcessRequest.Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);
                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.IsCompleted) throw new Exception("Tamamlanmış süreç revize edilemez.");
                var approval = request.Approvals.FirstOrDefault(x =>x.ApproverId == userId &&x.IsActive);
                if (approval == null) throw new Exception("Aktif onay bulunamadı.");
                // mevcut onayı kapat

                approval.Status =WorkflowStatus.Revize.ToString();
                approval.ActionDate =DateTime.Now;
                approval.Comment =comment;
                approval.IsActive =false;
                // mevcut ve sonraki bekleyen adımları kapat

                foreach (var item in request.Approvals
                    .Where(x =>
                        x.StepOrder >= approval.StepOrder
                        &&
                        x.Status == WorkflowStatus.Onayda.ToString()
                        &&
                        x.IsActive))
                {
                    item.IsActive = false;
                    item.Status =WorkflowStatus.Iptal.ToString();
                    item.ActionDate =DateTime.Now;
                }
                request.Status =WorkflowStatus.Revize.ToString();
                request.IsCompleted =false;
                request.CurrentStep =0;

                // İzin tablosunu güncelle

                var izin = await _context.IzinTalepleri.FirstOrDefaultAsync(x =>x.ProcessRequestId == request.Id);
                if (izin != null)
                {
                    izin.Durum =WorkflowStatus.Revize.ToString();
                    izin.GuncellemeTarihi =DateTime.Now;
                }
                AddHistory(request,userId,WorkflowAction.Revize,comment ?? "Talep revize istendi.");
                await _context.SaveChangesAsync();
                await _workflowNotificationService.SendWorkflowReturnedAsync(request.Id,comment);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task ResubmitAsync(int requestId,int userId,string? comment)
        {
            using var transaction =await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ProcessRequest.Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.Status != WorkflowStatus.Revize.ToString()) throw new Exception("Sadece revize durumundaki talepler tekrar gönderilebilir.");
                if (request.CreatedBy != userId) throw new Exception("Bu talebi sadece oluşturan kişi tekrar gönderebilir.");

                // Eski approval kayıtlarını kapat

                foreach (var approval in request.Approvals)
                {
                    approval.IsActive = false;
                    if (approval.Status ==WorkflowStatus.Onayda.ToString())
                    {
                        approval.Status =WorkflowStatus.Iptal.ToString();
                    }
                }

                var workflow =await GetWorkflowDefinitionAsync(request.ProcessTypeId);
                var approvals =await BuildApprovalsAsync(workflow,request);

                if (!approvals.Any()) throw new Exception("Workflow için onaylayıcı bulunamadı.");
                ActivateFirstStep(approvals);
                request.Status = WorkflowStatus.Onayda.ToString();
                request.CurrentStep = approvals.Min(x => x.StepOrder);
                request.IsCompleted = false;
                _context.ProcessApproval.AddRange(approvals);

                var izin =await _context.IzinTalepleri.FirstOrDefaultAsync(x =>x.ProcessRequestId == request.Id);

                if (izin != null)
                {
                    izin.Durum =WorkflowStatus.Onayda.ToString();
                    izin.GuncellemeTarihi =DateTime.Now;
                }
                AddHistory(request,userId,WorkflowAction.TekrarGonder,comment ?? "Revize sonrası tekrar gönderildi.");
                await _context.SaveChangesAsync();
                await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task CancelAsync(int requestId,int userId,string? comment)
        {
            using var transaction =await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ProcessRequest.Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);
                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.IsCompleted) throw new Exception("Tamamlanmış bir süreç iptal edilemez.");
                if (request.CreatedBy != userId) { throw new Exception("Bu talebi sadece oluşturan kişi iptal edebilir."); }
                // Bekleyen onayları kapat

                foreach (var approval in request.Approvals
                    .Where(x =>x.Status ==WorkflowStatus.Onayda.ToString() && x.IsActive))
                {
                    approval.IsActive = false;
                    approval.Status =WorkflowStatus.Iptal.ToString();
                    approval.ActionDate =DateTime.Now;
                    approval.Comment =comment;
                }
                request.Status =WorkflowStatus.Iptal.ToString();
                request.IsCompleted =true;
                request.CurrentStep =0;

                // İzin tablosunu güncelle
                var izin =await _context.IzinTalepleri.FirstOrDefaultAsync(x =>x.ProcessRequestId == request.Id);

                if (izin != null)
                {
                    izin.Durum =WorkflowStatus.Iptal.ToString();
                    izin.GuncellemeTarihi =DateTime.Now;
                }

                AddHistory(request,userId,WorkflowAction.Iptal,comment ?? "Talep iptal edildi.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<int> StartAndSubmitAsync(ProcessRequest request,string? comment = null)
        {
            var requestId = await StartAsync(request);

            await SubmitAsync(requestId,request.CreatedBy,comment ?? "Talep onaya gönderildi.");

            return requestId;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Workflow tanımını getirir.
        /// </summary>
        private async Task<WorkflowDefinition> GetWorkflowDefinitionAsync(int processTypeId)
        {
            var workflow = await _context.WorkflowDefinition
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(x =>
                    x.ProcessTypeId == processTypeId &&
                    x.IsActive);

            if (workflow == null)
                throw new Exception($"ProcessTypeId={processTypeId} için aktif workflow tanımı bulunamadı.");

            workflow.Steps = workflow.Steps
                .OrderBy(x => x.StepOrder)
                .ToList();

            return workflow;
        }

        /// <summary>
        /// Workflow adımlarına göre ProcessApproval kayıtlarını oluşturur.
        /// </summary>
        private async Task<List<ProcessApproval>> BuildApprovalsAsync(
            WorkflowDefinition workflow,
            ProcessRequest request)
        {
            List<ProcessApproval> approvals = new();

            int order = 1;


            foreach (var step in workflow.Steps.OrderBy(x => x.StepOrder))
            {
                var resolver = GetResolver(step.ApprovalType);


                var approvers = await resolver.ResolveAsync(
                    step,
                    request);


                if (!approvers.Any())
                    continue;



                foreach (var approverId in approvers)
                {
                    approvals.Add(new ProcessApproval
                    {
                        RequestId = request.Id,

                        ApproverId = approverId,

                        WorkflowStepId = step.Id,

                        StepOrder = order,

                        Status = WorkflowStatus.Onayda.ToString(),

                        IsActive = false
                    });


                    order++;
                }


                // Role/IK gibi Any adımlarında
                // aynı sıra numarası gerekli
                if (step.StepType == WorkflowStepType.Any)
                {
                    var lastOrder = order - 1;

                    foreach (var item in approvals
                        .Where(x => x.WorkflowStepId == step.Id))
                    {
                        item.StepOrder = lastOrder;
                    }
                }
            }


            return approvals;
        }
        /// <summary>
        /// İlk onay adımını aktif hale getirir.
        /// </summary>
        private void ActivateFirstStep(List<ProcessApproval> approvals)
        {
            if (approvals == null || !approvals.Any())
                return;


            var firstOrder = approvals
                .Min(x => x.StepOrder);


            foreach (var approval in approvals
                .Where(x => x.StepOrder == firstOrder))
            {
                approval.IsActive = true;
            }
        }

        /// <summary>
        /// ApprovalType'a göre ilgili resolver'ı döndürür.
        /// </summary>
        private IApprovalResolver GetResolver(ApprovalType approvalType)
        {
            var resolver = _resolvers.FirstOrDefault(x => x.ApprovalType == approvalType);

            if (resolver == null)
                throw new Exception($"{approvalType} için resolver bulunamadı.");

            return resolver;
        }

        #endregion

        private void AddHistory(ProcessRequest request, int userId, WorkflowAction action, string? comment)
        {
            request.Histories.Add(new WorkflowHistory
            {
                RequestId = request.Id,
                UserId = userId,
                ActionType = (int)action,
                ActionDate = DateTime.Now,
                Comment = comment
            });
        }

        private void ActivateNextStep(ProcessRequest request,int currentStep)
        {
            var nextStepOrder = request.Approvals
                .Where(x =>
                    x.StepOrder > currentStep &&
                    x.Status == WorkflowStatus.Onayda.ToString())
                .OrderBy(x => x.StepOrder)
                .Select(x => x.StepOrder)
                .FirstOrDefault();

            if (nextStepOrder == 0)
            {
                request.Status =
                    WorkflowStatus.Tamamlandi.ToString();

                request.IsCompleted = true;
                request.CurrentStep = 0;

                return;
            }

            foreach (var approval in request.Approvals
                .Where(x =>
                    x.StepOrder == nextStepOrder &&
                    x.Status == WorkflowStatus.Onayda.ToString()))
            {
                approval.IsActive = true;
            }

            request.CurrentStep = nextStepOrder;
        }
        private async Task IzinBakiyesiniDusAsync(IzinTalepleri izin)
        {
            if (izin.BakiyeDusuldu)
                return;

            // Sadece yıllık izin
            if (izin.IzinTuruId != 1)
                return;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == izin.PersonelId);

            if (user == null || string.IsNullOrWhiteSpace(user.Sicil))
                return;

            var bakiye = await _context.IzinYillikBakiyePersonel
                .FirstOrDefaultAsync(x => x.Sicil == user.Sicil);

            if (bakiye == null)
                return;

            bakiye.Bakiye -= izin.GunSayisi;

            izin.BakiyeDusuldu = true;
        }

    }
}