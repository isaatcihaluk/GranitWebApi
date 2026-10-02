using DocumentFormat.OpenXml.InkML;
using GranitWebApi.Data;
using GranitWebApi.Models.Proforma;
using GranitWebApi.Models.Sales;
using GranitWebApi.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using SalesProformaModel = GranitWebApi.Models.Proforma.SalesProforma;

namespace GranitWebApi.Services.Proforma
{
    public class SalesProformaService : ISalesProformaService
    {
        private readonly AppDbContext _context;
        private readonly WorkflowService _workflowService;

        public SalesProformaService(AppDbContext context, WorkflowService workflowService)
        {
            _context = context;
            _workflowService = workflowService;
        }

        #region List
        public async Task<List<SalesProformaModel>> GetListAsync()
        {
            return await _context.SalesProformas.AsNoTracking()
                .Include(x => x.Lines).Where(x => x.Status != "IPTAL")
                .OrderByDescending(x => x.Id).ToListAsync();
        }
        #endregion

        #region Get By Id
        public async Task<SalesProformaModel?> GetByIdAsync(int id)
        {
            return await _context.SalesProformas
                .AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        #endregion

        #region Create
        public async Task<SalesProformaModel> CreateAsync(SalesProformaModel proforma,int userId)
        {
            proforma.Id = 0;
            proforma.CreatedBy = userId;
            proforma.CreatedAt = DateTime.Now;

            if (string.IsNullOrWhiteSpace(proforma.Status))
            {
                proforma.Status = "TASLAK";
            }

            foreach (var line in proforma.Lines)
            {
                line.Id = 0;
                line.CreatedBy = userId;
                line.CreatedAt = DateTime.Now;
                line.TotalAmount = line.Quantity * line.UnitPrice;
            }

            proforma.TotalAmount = proforma.Lines.Where(x => !x.DeletedFlag).Sum(x => x.TotalAmount);
            _context.SalesProformas.Add(proforma);
            await _context.SaveChangesAsync();
            return proforma;
        }
        #endregion

        #region Update
        public async Task<SalesProformaModel?> UpdateAsync(int id,SalesProformaModel proforma,int userId)
        {
            var existing = await _context.SalesProformas
                .Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null) return null;

            // Header
            existing.ProformaYear =proforma.ProformaYear;
            existing.SalesRepresentativeUserId =proforma.SalesRepresentativeUserId;
            existing.CustomerCode =proforma.CustomerCode;
            existing.CustomerName =proforma.CustomerName;
            existing.SalesType =proforma.SalesType;
            existing.OrderType =proforma.OrderType;
            existing.CurrencyCode =proforma.CurrencyCode;
            existing.DueDate =proforma.DueDate;
            existing.DeliveryMethod =proforma.DeliveryMethod;
            existing.IncotermId =proforma.IncotermId;
            existing.PaymentType =proforma.PaymentType;
            existing.AdvancePercentage =proforma.AdvancePercentage;
            existing.PaymentTermDays =proforma.PaymentTermDays;
            existing.PaymentDescription =proforma.PaymentDescription;
            existing.Description =proforma.Description;
            existing.UpdatedAt =DateTime.Now;
            existing.UpdatedBy =userId;

            // Eski satırları soft delete
            foreach (var oldLine in existing.Lines)
            {
                oldLine.DeletedFlag = true;
                oldLine.UpdatedAt = DateTime.Now;
                oldLine.UpdatedBy = userId;
            }

            // Yeni satırlar
            foreach (var line in proforma.Lines)
            {
                var newLine = new SalesProformaLine
                {
                    ProformaId = id,
                    ProductGroupId = line.ProductGroupId,
                    ProductId = line.ProductId,
                    ProductCode = line.ProductCode,
                    ProductName = line.ProductName,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    Unit = line.Unit,
                    UnitPrice = line.UnitPrice,
                    CurrencyCode = line.CurrencyCode,
                    TotalAmount =
                        line.Quantity * line.UnitPrice,
                    DeletedFlag = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId
                };

                existing.Lines.Add(newLine);
            }

            existing.TotalAmount =
                existing.Lines
                    .Where(x => !x.DeletedFlag)
                    .Sum(x => x.TotalAmount);

            await _context.SaveChangesAsync();

            return existing;
        }
        #endregion

        #region Workflow
        public async Task<int> GetProcessTypeIdAsync()
        {
            var processType =
                await _context.ProcessTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Code == "SATIS_PROFORMA");

            if (processType == null)
            {
                throw new Exception("SATIS_PROFORMA process type bulunamadı.");
            }
            return processType.Id;
        }

        public async Task<bool> SetWorkflowAsync(int id,int processRequestId,string status,int userId)
        {
            var proforma = await _context.SalesProformas.FirstOrDefaultAsync(x => x.Id == id);
            if (proforma == null) return false;

            proforma.ProcessRequestId = processRequestId;
            proforma.Status = status; 
            proforma.UpdatedAt = DateTime.Now;
            proforma.UpdatedBy = userId;
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Status

        public async Task<bool> UpdateStatusAsync(int id,string status,int userId)
        {
            var proforma = await _context.SalesProformas.FirstOrDefaultAsync(x => x.Id == id);
            if (proforma == null) return false;

            proforma.Status = status;
            proforma.UpdatedAt = DateTime.Now;
            proforma.UpdatedBy = userId;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Payment
        public async Task<bool> CreatePaymentAsync(int proformaId,SalesProformaPaymentRequest request,int userId)
        {
            var proforma = await _context.SalesProformas.FirstOrDefaultAsync(x => x.Id == proformaId);

            if (proforma == null) return false;

            if (proforma.PaymentType != "PESIN" && proforma.PaymentType != "ON_ODEMELI")
            {
                throw new Exception("Bu proforma için ön ödeme kaydı oluşturulamaz.");
            }
            if (proforma.Status != "ON_ODEME_BEKLIYOR")
            {
                throw new Exception("Proforma ödeme bekleme durumunda değil.");
            }
            if (proforma.CreatedBy != userId)
            {
                throw new Exception("Bu proformanın ödeme bilgisini yalnızca proformayı oluşturan kullanıcı güncelleyebilir.");
            }
            if (request.PaidAmount <= 0)
            {
                throw new Exception("Gelen ödeme tutarı 0'dan büyük olmalıdır.");
            }
            if (proforma.AdvancePercentage == null || proforma.AdvancePercentage <= 0)
            {
                throw new Exception("Proforma için ön ödeme oranı tanımlanmamış.");
            }

            var expectedAmount = proforma.TotalAmount * proforma.AdvancePercentage.Value / 100;
            var payment = await _context.SalesProformaPayments.FirstOrDefaultAsync(x => x.ProformaId == proformaId);

            if (payment == null)
            {
                payment = new SalesProformaPayment
                {
                    ProformaId = proformaId,
                    ExpectedAmount = expectedAmount,
                    PaidAmount = request.PaidAmount,
                    CurrencyCode = proforma.CurrencyCode,
                    PaymentDate = request.PaymentDate,
                    PaymentDescription = request.PaymentDescription,
                    Status = "ODEME_BILDIRILDI",
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId
                };

                _context.SalesProformaPayments.Add(payment);
            }
            else
            {
                payment.ExpectedAmount = expectedAmount;
                payment.PaidAmount = request.PaidAmount;
                payment.CurrencyCode = proforma.CurrencyCode;
                payment.PaymentDate = request.PaymentDate;
                payment.PaymentDescription = request.PaymentDescription;
                if (payment.Status != "REVIZE")
                {
                    payment.Status = "ODEME_BILDIRILDI";
                }

                payment.UpdatedAt = DateTime.Now;
                payment.UpdatedBy = userId;
            }
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Submit Payment For Finance

        public async Task<bool> SubmitPaymentForFinanceAsync(int proformaId,int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payment = await _context.SalesProformaPayments
                        .Where(x => x.ProformaId == proformaId && x.Status == "ODEME_BILDIRILDI")
                        .OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                if (payment == null)
                {
                    throw new Exception("Finans onayına gönderilecek ödeme kaydı bulunamadı.");
                }

                var proforma = await _context.SalesProformas.FirstOrDefaultAsync(x => x.Id == proformaId);
                if (proforma == null)
                {
                    throw new Exception("Proforma bulunamadı.");
                }
                if (proforma.Status != "ON_ODEME_BEKLIYOR")
                {
                    throw new Exception("Proforma ödeme bekleme durumunda değil.");
                }
                if (proforma.CreatedBy != userId)
                {
                    throw new Exception("Bu proformanın ödemesini yalnızca proformayı oluşturan kullanıcı finans onayına gönderebilir.");
                }
                if (proforma.ProcessRequestId == null)
                {
                    throw new Exception("Proformaya ait workflow süreci bulunamadı.");
                }

                payment.Status ="FINANS_ONAYINDA";
                payment.UpdatedAt =DateTime.Now;
                payment.UpdatedBy =userId;
                proforma.Status ="FINANS_ONAY_BEKLIYOR";
                proforma.UpdatedAt =DateTime.Now;
                proforma.UpdatedBy =userId;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Convert To Order

        public async Task<SalesOrder?> ConvertToOrderAsync(int proformaId,int userId)
        {
            var proforma = await _context.SalesProformas.FirstOrDefaultAsync(x => x.Id == proformaId);
            if (proforma == null) return null;
            if (proforma.Status != "TAMAMLANDI")
            {
                throw new Exception("Yalnızca TAMAMLANDI durumundaki proformalar siparişe dönüştürülebilir.");
            }
            if (proforma.Ordered)
            {
                throw new Exception("Bu proforma daha önce siparişe dönüştürülmüş.");
            }
            if (string.IsNullOrWhiteSpace(proforma.SystemProformaNumber))
            {
                throw new Exception("Proformanın sistem numarası bulunamadı.");
            }
            if (proforma.SystemProformaNumber.Length < 3)
            {
                throw new Exception("Proformanın sistem numarası geçersiz.");
            }

            string systemOrderNumber;
            if (proforma.OrderType == "Yurtdışı")
            {
                systemOrderNumber = "EXP" + proforma.SystemProformaNumber.Substring(3);
            }
            else if (proforma.OrderType == "Yurtiçi")
            {
                systemOrderNumber = "ICS" + proforma.SystemProformaNumber.Substring(3);
            }
            else
            {
                throw new Exception($"Proforma tipi geçersiz: {proforma.OrderType}");
            }

            // Aynı proformadan daha önce sipariş oluşturulmuş mu?
            var existingOrder = await _context.SalesOrders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SystemOrderNumber == systemOrderNumber);

            if (existingOrder != null)
            {
                throw new Exception(
                    $"Bu proforma daha önce siparişe dönüştürülmüş. " +
                    $"Sipariş: {existingOrder.SystemOrderNumber}");
            }

            var order = new SalesOrder
            {
                SalesRepresentativeUserId = proforma.SalesRepresentativeUserId,
                OrderNumber = proforma.ProformaNumber,
                OrderYear = proforma.ProformaYear,
                SystemOrderNumber = systemOrderNumber,
                SalesType = proforma.SalesType ?? string.Empty,
                OrderType = proforma.OrderType ?? string.Empty,
                CustomerCode = proforma.CustomerCode,
                CustomerName = proforma.CustomerName,
                DeliveryMethod = proforma.DeliveryMethod ?? string.Empty,
                IncotermId = proforma.IncotermId,
                DueDate = proforma.DueDate,
                CurrencyCode = proforma.CurrencyCode,
                Status = "TASLAK",
                CreatedAt = DateTime.Now,
                CreatedBy = userId
            };

            _context.SalesOrders.Add(order);
            proforma.Ordered = true;
            proforma.Status = "SIPARISLESTIRILDI";
            await _context.SaveChangesAsync();
            return order;
        }

        #endregion
    }
}