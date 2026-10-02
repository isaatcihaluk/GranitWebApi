using GranitWebApi.Data;
using GranitWebApi.Services.Notifications;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GranitWebApi.Workflow.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly AppDbContext _context;
        private readonly IEnumerable<IApprovalResolver> _resolvers;
        private readonly WorkflowNotificationService _workflowNotificationService;
        private readonly IEnumerable<IWorkflowProcessHandler> _processHandlers;

        public WorkflowService(AppDbContext context,IEnumerable<IApprovalResolver> resolvers,
            WorkflowNotificationService workflowNotificationService,IEnumerable<IWorkflowProcessHandler> processHandlers)
        {
            _context = context;
            _resolvers = resolvers;
            _workflowNotificationService = workflowNotificationService;
            _processHandlers = processHandlers;
        }

        #region Start

        public async Task<int> StartAsync(ProcessRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                request.Status = WorkflowStatus.Taslak.ToString();
                request.CreatedDate = DateTime.Now;
                request.IsCompleted = false;
                request.CurrentStep = 0;

                _context.ProcessRequest.Add(request);
                await _context.SaveChangesAsync();
                AddHistory(request,request.CreatedBy,WorkflowAction.Baslat,"Talep oluşturuldu.");
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

        #endregion

        #region Submit

        public async Task SubmitAsync(int requestId,int userId,string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");

                if (request.CreatedBy != userId) throw new Exception("Sadece talep sahibi gönderebilir.");

                if (request.Status != WorkflowStatus.Taslak.ToString())
                {
                    throw new Exception("Sadece taslak durumundaki talepler gönderilebilir.");
                }

                var workflow = await GetWorkflowDefinitionAsync(request.ProcessTypeId);
                var approvals = await BuildApprovalsAsync(workflow,request);
                if (!approvals.Any())
                {
                    throw new Exception("Workflow için onaylayıcı bulunamadı.");
                }

                ActivateFirstStep(approvals);

                request.Status = WorkflowStatus.Onayda.ToString();
                request.CurrentStep = approvals.Min(x => x.StepOrder);
                request.IsCompleted = false;
                _context.ProcessApproval.AddRange(approvals);

                AddHistory(request,userId,WorkflowAction.Gonder,comment ?? "Talep onaya gönderildi.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Workflow bildirim gönderilemedi: {ex.Message}");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Approve

        public async Task ApproveAsync(int requestId,int userId,string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.IsCompleted) throw new Exception("Bu süreç zaten tamamlanmış.");
                if (request.Status != WorkflowStatus.Onayda.ToString())
                {
                    throw new Exception("Bu süreç onay durumunda değil.");
                }

                var approval = request.Approvals.FirstOrDefault(x =>
                    x.ApproverId == userId && x.IsActive &&
                    x.Status == WorkflowApprovalStatus.Onayda.ToString());

                if (approval == null)
                {
                    throw new Exception("Bu kullanıcının aktif onayı bulunmamaktadır.");
                }

                var workflowStep = await _context.WorkflowStep.FirstOrDefaultAsync(x => x.Id == approval.WorkflowStepId);
                if (workflowStep == null) throw new Exception("Workflow adımı bulunamadı.");

                approval.Status = WorkflowApprovalStatus.Onaylandi.ToString();
                approval.ActionDate = DateTime.Now;
                approval.Comment = comment;
                approval.IsActive = false;

                bool stepCompleted = false;

                switch (workflowStep.StepType)
                {
                    case WorkflowStepType.Any:

                        foreach (var item in request.Approvals.Where(x =>
                            x.WorkflowStepId == approval.WorkflowStepId &&
                            x.Status == WorkflowApprovalStatus.Onayda.ToString() && x.IsActive))
                        {
                            item.IsActive = false;
                            item.Status = WorkflowApprovalStatus.Iptal.ToString();
                            item.ActionDate = DateTime.Now;
                        }

                        stepCompleted = true;
                        break;

                    case WorkflowStepType.Parallel:

                        var pendingParallel = request.Approvals.Any(x => x.WorkflowStepId == approval.WorkflowStepId &&
                                x.Status == WorkflowApprovalStatus.Onayda.ToString() && x.IsActive);

                        if (!pendingParallel)
                        {
                            stepCompleted = true;
                        }

                        break;

                    default:

                        stepCompleted = true;

                        break;
                }

                WorkflowStepResult? stepResult = null;

                if (stepCompleted)
                {
                    var handler = await GetProcessHandlerAsync(request.ProcessTypeId);
                    if (handler != null)
                    {
                        stepResult = await handler.OnStepCompletedAsync(request,approval.StepOrder,userId);
                    }
                    if (stepResult == null || !stepResult.PauseWorkflow)
                    {
                        await ActivateNextStep(request,approval.StepOrder);
                    }
                    else
                    {
                        request.Status = WorkflowStatus.Onayda.ToString();
                        request.CurrentStep = stepResult.CurrentStep;
                    }
                }

                AddHistory(request,userId,WorkflowAction.Onayla,comment ?? "Talep onaylandı.");

                if (request.IsCompleted)
                {
                    var handler = await GetProcessHandlerAsync(request.ProcessTypeId);
                    if (handler != null)
                    {
                        await handler.OnCompletedAsync(request,userId);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                if (request.IsCompleted)
                {
                    try
                    {
                        await _workflowNotificationService.SendWorkflowCompletedAsync(request.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Workflow tamamlandı bildirimi gönderilemedi: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Workflow bildirim gönderilemedi: {ex.Message}");
                    }
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Reject

        public async Task RejectAsync(int requestId,int userId,string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.IsCompleted)
                {
                    throw new Exception("Tamamlanmış süreç reddedilemez.");
                }
                if (request.Status != WorkflowStatus.Onayda.ToString())
                {
                    throw new Exception("Bu süreç onay durumunda değil.");
                }

                var approval = request.Approvals.FirstOrDefault(x => x.ApproverId == userId &&
                    x.IsActive && x.Status == WorkflowApprovalStatus.Onayda.ToString());

                if (approval == null) throw new Exception("Aktif onay bulunamadı.");

                approval.Status = WorkflowApprovalStatus.Reddedildi.ToString();
                approval.ActionDate = DateTime.Now;
                approval.Comment = comment;
                approval.IsActive = false;

                foreach (var item in request.Approvals.Where(x =>
                    x.Status == WorkflowApprovalStatus.Onayda.ToString() && x.IsActive))
                {
                    item.IsActive = false;
                    item.Status = WorkflowApprovalStatus.Iptal.ToString();
                    item.ActionDate = DateTime.Now;
                }

                request.Status = WorkflowStatus.Reddedildi.ToString();
                request.IsCompleted = true;
                request.CurrentStep = 0;

                AddHistory(request,userId,WorkflowAction.Reddet,comment ?? "Talep reddedildi.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _workflowNotificationService.SendWorkflowRejectedAsync(request.Id,comment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Workflow red bildirimi gönderilemedi: {ex.Message}");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Return / Revision

        public async Task ReturnAsync(int requestId,int userId,string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");

                if (request.IsCompleted)
                {
                    throw new Exception("Tamamlanmış süreç revize edilemez.");
                }

                if (request.Status != WorkflowStatus.Onayda.ToString())
                {
                    throw new Exception("Yalnızca onay durumundaki süreç revize edilebilir.");
                }

                var approval = request.Approvals.FirstOrDefault(x => x.ApproverId == userId &&
                    x.IsActive && x.Status == WorkflowApprovalStatus.Onayda.ToString());

                if (approval == null) throw new Exception("Aktif onay bulunamadı.");

                approval.Status = WorkflowApprovalStatus.Revize.ToString();
                approval.ActionDate = DateTime.Now;
                approval.Comment = comment;
                approval.IsActive = false;

                foreach (var item in request.Approvals.Where(x =>
                    x.StepOrder >= approval.StepOrder &&
                    x.Status == WorkflowApprovalStatus.Onayda.ToString() && x.IsActive))
                {
                    item.IsActive = false;
                    item.Status = WorkflowApprovalStatus.Iptal.ToString();
                    item.ActionDate = DateTime.Now;
                }

                request.Status = WorkflowStatus.Revize.ToString();
                request.IsCompleted = false;
                request.CurrentStep = approval.StepOrder;

                var handler = await GetProcessHandlerAsync(request.ProcessTypeId);

                if (handler != null)
                {
                    await handler.OnReturnedAsync(request,approval.StepOrder, userId);
                }

                AddHistory(request,userId,WorkflowAction.Revize,comment ?? "Talep revize istendi.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _workflowNotificationService.SendWorkflowReturnedAsync(request.Id,comment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Workflow revize bildirimi gönderilemedi: {ex.Message}");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Resubmit

        public async Task ResubmitAsync(int requestId,int userId,string? comment,string? parameters = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals) .FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.Status != WorkflowStatus.Revize.ToString())
                {
                    throw new Exception("Sadece revize durumundaki talepler tekrar gönderilebilir.");
                }
                if (request.CreatedBy != userId)
                {
                    throw new Exception("Bu talebi sadece oluşturan kişi tekrar gönderebilir.");
                }

                var revisionStep = request.CurrentStep;
                if (revisionStep <= 0)
                {
                    throw new Exception("Revize edilen workflow adımı belirlenemedi.");
                }
                if (parameters != null)
                {
                    request.Parameters = parameters;
                }

                foreach (var approval in request.Approvals)
                {
                    approval.IsActive = false;

                    if (approval.Status == WorkflowApprovalStatus.Onayda.ToString())
                    {
                        approval.Status = WorkflowApprovalStatus.Iptal.ToString();
                        approval.ActionDate = DateTime.Now;
                    }
                }

                var workflow = await GetWorkflowDefinitionAsync(request.ProcessTypeId);
                var approvals = await BuildApprovalsAsync(workflow,request);
                if (!approvals.Any())
                {
                    throw new Exception("Workflow için onaylayıcı bulunamadı.");
                }

                var revisionApprovals = approvals.Where(x => x.StepOrder == revisionStep).ToList();
                if (!revisionApprovals.Any())
                {
                    throw new Exception($"Revize edilen workflow adımı bulunamadı. StepOrder: {revisionStep}");
                }

                var handler = await GetProcessHandlerAsync(request.ProcessTypeId);

                WorkflowResubmitResult? resubmitResult = null;
                if (handler != null)
                {
                    resubmitResult = await handler.OnResubmittedAsync(request,revisionStep,userId);
                }

                if (resubmitResult == null || !resubmitResult.PauseWorkflow)
                {
                    foreach (var approval in revisionApprovals)
                    {
                        approval.IsActive = true;
                    }

                    request.Status = WorkflowStatus.Onayda.ToString();
                    request.CurrentStep = revisionStep;
                    request.IsCompleted = false;
                }
                else
                {
                    request.Status = WorkflowStatus.Onayda.ToString();
                    request.CurrentStep = resubmitResult.CurrentStep;
                    request.IsCompleted = false;
                }

                _context.ProcessApproval.AddRange(approvals);

                AddHistory(request,userId,WorkflowAction.TekrarGonder,comment ?? "Revize sonrası tekrar gönderildi.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Workflow tekrar gönderim bildirimi gönderilemedi: {ex.Message}");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Cancel

        public async Task CancelAsync(int requestId,int userId,string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ProcessRequest
                    .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

                if (request == null) throw new Exception("Talep bulunamadı.");
                if (request.IsCompleted)
                {
                    throw new Exception("Tamamlanmış bir süreç iptal edilemez.");
                }
                if (request.CreatedBy != userId)
                {
                    throw new Exception("Bu talebi sadece oluşturan kişi iptal edebilir.");
                }

                foreach (var approval in request.Approvals.Where(x =>
                    x.Status == WorkflowApprovalStatus.Onayda.ToString() && x.IsActive))
                {
                    approval.IsActive = false;
                    approval.Status = WorkflowApprovalStatus.Iptal.ToString();
                    approval.ActionDate = DateTime.Now;
                    approval.Comment = comment;
                }

                request.Status = WorkflowStatus.Iptal.ToString();
                request.IsCompleted = true;
                request.CurrentStep = 0;

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

        #endregion

        #region Start And Submit

        public async Task<int> StartAndSubmitAsync(ProcessRequest request,string? comment = null)
        {
            var requestId = await StartAsync(request);
            await SubmitAsync(requestId,request.CreatedBy,comment ?? "Talep onaya gönderildi.");
            return requestId;
        }

        #endregion

        #region Activate Step

        public async Task ActivateStepAsync(int requestId,int stepOrder,int userId,string? comment = null)
        {
            var request = await _context.ProcessRequest
                .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null) throw new Exception("Talep bulunamadı.");
            if (request.IsCompleted) throw new Exception("Tamamlanmış süreçte adım başlatılamaz.");
            if (request.Status != WorkflowStatus.Onayda.ToString())
            {
                throw new Exception("Workflow onay durumunda değil.");
            }
            if (request.CurrentStep != 0)
            {
                throw new Exception("Workflow manuel adım başlatmaya uygun durumda değil.");
            }

            var approvals = request.Approvals
                .Where(x => x.StepOrder == stepOrder &&
                    x.Status == WorkflowApprovalStatus.Onayda.ToString()).ToList();

            if (!approvals.Any())
            {
                throw new Exception($"Aktif hale getirilecek workflow adımı bulunamadı. StepOrder: {stepOrder}");
            }

            foreach (var approval in approvals)
            {
                approval.IsActive = true;
            }

            request.CurrentStep = stepOrder;
            request.Status = WorkflowStatus.Onayda.ToString();

            AddHistory(request,userId,WorkflowAction.Gonder,comment ?? $"Workflow {stepOrder}. adıma gönderildi.");

            await _context.SaveChangesAsync();

            try
            {
                await _workflowNotificationService.SendWorkflowSubmittedAsync(request.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Workflow adım bildirimi gönderilemedi: {ex.Message}");
            }
        }

        #endregion

        #region Workflow Definition

        private async Task<WorkflowDefinition>GetWorkflowDefinitionAsync(int processTypeId)
        {
            var workflow = await _context.WorkflowDefinition.Include(x => x.Steps)
                    .FirstOrDefaultAsync(x => x.ProcessTypeId == processTypeId && x.IsActive);

            if (workflow == null)
            {
                throw new Exception($"ProcessTypeId={processTypeId} için aktif workflow tanımı bulunamadı.");
            }

            workflow.Steps = workflow.Steps.OrderBy(x => x.StepOrder).ToList();
            return workflow;
        }

        #endregion

        #region Build Approvals

        private async Task<List<ProcessApproval>>BuildApprovalsAsync(WorkflowDefinition workflow,ProcessRequest request)
        {
            List<ProcessApproval> approvals = new();

            foreach (var step in workflow.Steps.OrderBy(x => x.StepOrder))
            {
                if (!ShouldRunStep(step, request)) continue;
                var resolver = GetResolver(step.ApprovalType);
                var approvers = await resolver.ResolveAsync(step,request);

                if (!approvers.Any()) continue;

                foreach (var approverId in approvers)
                {
                    approvals.Add(new ProcessApproval
                    {
                        RequestId = request.Id,
                        ApproverId = approverId,
                        WorkflowStepId = step.Id,
                        StepOrder = step.StepOrder,
                        Status = WorkflowApprovalStatus.Onayda.ToString(),
                        IsActive = false
                    });
                }
            }

            return approvals;
        }

        #endregion

        #region Activate First Step

        private void ActivateFirstStep(List<ProcessApproval> approvals)
        {
            if (approvals == null || !approvals.Any()) return;
            var firstOrder = approvals.Min(x => x.StepOrder);
            foreach (var approval in approvals.Where(x => x.StepOrder == firstOrder))
            {
                approval.IsActive = true;
            }
        }

        #endregion

        #region Resolver

        private IApprovalResolver GetResolver(ApprovalType approvalType)
        {
            var resolver = _resolvers.FirstOrDefault(x => x.ApprovalType == approvalType);
            if (resolver == null)
            {
                throw new Exception($"{approvalType} için resolver bulunamadı.");
            }
            return resolver;
        }

        #endregion

        #region Process Handler
        private async Task<IWorkflowProcessHandler?>GetProcessHandlerAsync(int processTypeId)
        {
            var processType =await _context.ProcessTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == processTypeId);

            if (processType == null)
            {
                throw new Exception($"ProcessType bulunamadı. Id: {processTypeId}");
            }
            return _processHandlers.FirstOrDefault(x => x.ProcessTypeCode == processType.Code);
        }

        #endregion

        #region History
        private void AddHistory(ProcessRequest request,int userId,WorkflowAction action,string? comment)
        {
            request.Histories.Add(
                new WorkflowHistory
                {
                    RequestId = request.Id,
                    UserId = userId,
                    ActionType = (int)action,
                    ActionDate = DateTime.Now,
                    Comment = comment
                });
        }
        #endregion

        #region Activate Next Step
        private async Task ActivateNextStep(ProcessRequest request,int currentStep)
        {
            var nextStepOrder =request.Approvals
                    .Where(x => x.StepOrder > currentStep &&
                        x.Status == WorkflowApprovalStatus.Onayda.ToString())
                    .OrderBy(x => x.StepOrder).Select(x => x.StepOrder).FirstOrDefault();

            if (nextStepOrder == 0)
            {
                request.Status = WorkflowStatus.Tamamlandi.ToString();
                request.IsCompleted = true;
                request.CurrentStep = 0;

                return;
            }

            foreach (var approval in request.Approvals.Where(x =>
                x.StepOrder == nextStepOrder &&
                x.Status == WorkflowApprovalStatus.Onayda.ToString()))
            {
                approval.IsActive = true;
            }

            request.Status = WorkflowStatus.Onayda.ToString();
            request.CurrentStep = nextStepOrder;
            await Task.CompletedTask;
        }
        #endregion

        #region Step Conditions

        private bool ShouldRunStep(WorkflowStep step,ProcessRequest request)
        {
            if (string.IsNullOrWhiteSpace(step.Parameters)) return true;
            if (string.IsNullOrWhiteSpace(request.Parameters)) return false;

            using var stepDoc = JsonDocument.Parse(step.Parameters);
            using var requestDoc = JsonDocument.Parse(request.Parameters);

            var stepRoot = stepDoc.RootElement;
            var requestRoot =requestDoc.RootElement;
            if (stepRoot.ValueKind !=JsonValueKind.Object)
            {
                return true;
            }            

            foreach (var condition in stepRoot.EnumerateObject())
            {
                var conditionName = condition.Name;
                JsonElement requestProperty;
                bool found = requestRoot.TryGetProperty(conditionName,out requestProperty);
                if (!found && conditionName.EndsWith("s",StringComparison.OrdinalIgnoreCase))
                {
                    var singularName = conditionName[..^1];
                    found = requestRoot.TryGetProperty(singularName,out requestProperty);
                }

                if (!found) return false;

                if (condition.Value.ValueKind == JsonValueKind.Array)
                {
                    var requestValue = requestProperty.GetString();

                    if (requestValue == null) return false;

                    var matched = condition.Value.EnumerateArray()
                            .Any(x =>string.Equals(x.GetString(),requestValue,StringComparison.OrdinalIgnoreCase));

                    if (!matched) return false;
                }
                else
                {
                    var expected = condition.Value.ToString();
                    var actual = requestProperty.ToString();
                    if (!string.Equals(expected,actual,StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}