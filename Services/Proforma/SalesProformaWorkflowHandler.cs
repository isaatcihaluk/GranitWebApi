using GranitWebApi.Data;
using GranitWebApi.Services.Notifications;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GranitWebApi.Services.Proforma
{
    public class SalesProformaWorkflowHandler : IWorkflowProcessHandler
    {
        private readonly AppDbContext _context;
        private readonly WorkflowNotificationService _notificationService;

        public SalesProformaWorkflowHandler(AppDbContext context, WorkflowNotificationService notificationService)
        {
            _context = context; 
            _notificationService = notificationService;
        }

        public string ProcessTypeCode =>
            "SATIS_PROFORMA";

        #region Step Completed

        public async Task<WorkflowStepResult>
            OnStepCompletedAsync(
                ProcessRequest request,
                int completedStep,
                int userId)
        {
            var result = new WorkflowStepResult
            {
                PauseWorkflow = false,
                CurrentStep = completedStep
            };

            if (request.EntityId == null)
                return result;

            var proforma =
                await _context.SalesProformas
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.EntityId.Value);

            if (proforma == null)
            {
                throw new Exception(
                    $"Workflow'a bağlı proforma bulunamadı. " +
                    $"ProformaId: {request.EntityId}");
            }

            /*
             * 1. yönetici onayı
             *
             * Peşin / ön ödemeli ise:
             * Workflow burada durur.
             *
             * Satışçı ödeme bilgisini girer.
             * Daha sonra finans step'i manuel olarak
             * aktive edilir.
             */
            if (completedStep == 1 &&
                IsPrepaymentWorkflow(request))
            {
                proforma.Status =
                    "ON_ODEME_BEKLIYOR";

                proforma.UpdatedAt =
                    DateTime.Now;

                proforma.UpdatedBy =
                    userId;

                result.PauseWorkflow = true;
                result.CurrentStep = 0;
                await _notificationService.SendPrepaymentRequiredAsync(request.Id);

                return result;
            }

            /*
             * Finans onayı tamamlandı.
             */
            if (completedStep == 2 &&
                IsPrepaymentWorkflow(request))
            {
                var payment =
                    await _context.SalesProformaPayments
                        .Where(x =>
                            x.ProformaId ==
                                request.EntityId.Value &&
                            x.Status ==
                                "FINANS_ONAYINDA")
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                if (payment != null)
                {
                    payment.Status =
                        "ONAYLANDI";

                    payment.UpdatedAt =
                        DateTime.Now;

                    payment.UpdatedBy =
                        userId;
                }

                /*
                 * Finans onayından sonra workflow
                 * 3. adıma geçecektir.
                 *
                 * Proforma henüz tamamlanmadığı için
                 * nihai durum TAMAMLANDI değildir.
                 */
                proforma.Status =
                    "FINANS_ONAY_BEKLIYOR";

                proforma.UpdatedAt =
                    DateTime.Now;

                proforma.UpdatedBy =
                    userId;
            }

            return result;
        }

        #endregion

        #region Workflow Completed

        public async Task OnCompletedAsync(
            ProcessRequest request,
            int userId)
        {
            if (request.EntityId == null)
                return;

            var proforma =
                await _context.SalesProformas
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.EntityId.Value);

            if (proforma == null)
            {
                throw new Exception(
                    $"Workflow tamamlanırken proforma bulunamadı. " +
                    $"ProformaId: {request.EntityId}");
            }

            /*
             * Nihai proforma durumu.
             */
            proforma.Status =
                "TAMAMLANDI";

            proforma.UpdatedAt =
                DateTime.Now;

            proforma.UpdatedBy =
                userId;

            await Task.CompletedTask;
        }

        #endregion

        #region Returned

        public async Task OnReturnedAsync(
    ProcessRequest request,
    int revisionStep,
    int userId)
        {
            if (request.EntityId == null)
                return;

            // İlk onay aşamasında revize
            if (revisionStep == 1)
            {
                var proforma =
                    await _context.SalesProformas
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.EntityId.Value);

                if (proforma != null)
                {
                    proforma.Status = "REVIZE";
                    proforma.UpdatedAt = DateTime.Now;
                    proforma.UpdatedBy = userId;
                }

                return;
            }

            // Finans ödeme aşamasında revize
            if (revisionStep == 2 &&
                IsPrepaymentWorkflow(request))
            {
                var payment =
                    await _context.SalesProformaPayments
                        .Where(x =>
                            x.ProformaId == request.EntityId.Value)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                if (payment != null)
                {
                    payment.Status = "REVIZE";
                    payment.UpdatedAt = DateTime.Now;
                    payment.UpdatedBy = userId;
                }

                var proforma =
                    await _context.SalesProformas
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.EntityId.Value);

                if (proforma != null)
                {
                    proforma.Status = "ON_ODEME_BEKLIYOR";
                    proforma.UpdatedAt = DateTime.Now;
                    proforma.UpdatedBy = userId;
                }
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Resubmitted

        public async Task<WorkflowResubmitResult>
            OnResubmittedAsync(
                ProcessRequest request,
                int revisionStep,
                int userId)
        {
            var result =
                new WorkflowResubmitResult
                {
                    PauseWorkflow = false,
                    CurrentStep = revisionStep
                };

            if (request.EntityId == null)
                return result;

            /*
             * İlk onay adımı revize edilmişse:
             *
             * Yönetici -> REVIZE
             * Satışçı proformayı düzenler
             * -> Tekrar Onaya Gönder
             *
             * Proforma tekrar ONAY_BEKLIYOR durumuna alınır.
             */
            if (revisionStep == 1)
            {
                var proforma =
                    await _context.SalesProformas
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.EntityId.Value);

                if (proforma != null)
                {
                    proforma.Status = "ONAY_BEKLIYOR";
                    proforma.UpdatedAt = DateTime.Now;
                    proforma.UpdatedBy = userId;
                }
            }

            /*
             * Finans step'i revize edilmişse:
             *
             * Finans -> REVIZE
             * Satışçı ödeme bilgisini düzeltir
             * -> ODEME_BILDIRILDI
             *
             * Workflow burada bekler.
             *
             * Daha sonra satışçı:
             *
             * ActivateStepAsync(requestId, 2, ...)
             *
             * çağırır.
             */
            if (revisionStep == 2 &&
                IsPrepaymentWorkflow(request))
            {
                var payment =
                    await _context.SalesProformaPayments
                        .Where(x =>
                            x.ProformaId ==
                                request.EntityId.Value &&
                            x.Status == "REVIZE")
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                if (payment != null)
                {
                    payment.Status =
                        "ODEME_BILDIRILDI";

                    payment.UpdatedAt =
                        DateTime.Now;

                    payment.UpdatedBy =
                        userId;
                }

                result.PauseWorkflow = true;
                result.CurrentStep = 0;
            }

            return result;
        }

        #endregion

        #region Payment Workflow

        private bool IsPrepaymentWorkflow(
            ProcessRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                request.Parameters))
            {
                return false;
            }

            using var doc =
                JsonDocument.Parse(
                    request.Parameters);

            if (!doc.RootElement.TryGetProperty(
                    "PaymentType",
                    out var paymentType))
            {
                return false;
            }

            var value =
                paymentType.GetString();

            return value == "PESIN" ||
                   value == "ON_ODEMELI";
        }

        #endregion
    }
}