using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Sales;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly AppDbContext _context;
        private readonly ISalesOrderService _;
        private readonly IWorkflowService _workflowService;
        private readonly ErpDbContext _erpContext;
        private readonly UserContext _userContext;
        public SalesOrderService(AppDbContext context, UserContext userContext, ErpDbContext erpContext, IWorkflowService workflowService)
        {
            _context = context;
            _userContext = userContext;
            _erpContext = erpContext;
            _workflowService = workflowService;
        }
        private class SystemOrderNumberResult
        {
            public string SystemOrderNumber { get; set; } = null!;
        }
        public async Task<SalesOrder> CreateAsync(SalesOrder salesOrder,int userId)
        {
            // Sipariş yılı girilmemişse mevcut yılı kullan
            if (salesOrder.OrderYear <= 0) {salesOrder.OrderYear = DateTime.Now.Year;}
            salesOrder.CreatedBy = userId;
            salesOrder.CreatedAt = DateTime.Now;
            salesOrder.Status = "TASLAK";

            salesOrder.SystemOrderNumber = await CreateSystemOrderNumberAsync(salesOrder.OrderType,salesOrder.OrderNumber,salesOrder.OrderYear);
            var systemOrderNumberUsed = await IsSystemOrderNumberUsedAsync(salesOrder.SystemOrderNumber);

            if (systemOrderNumberUsed)
            {
                throw new Exception(
                    $"Sistem sipariş numarası daha önce kullanılmış. " +
                    $"Sistem Sipariş No: {salesOrder.SystemOrderNumber}");
            }
            _context.SalesOrders.Add(salesOrder);
            await _context.SaveChangesAsync();
            return salesOrder;
        }
        public async Task<List<SalesOrder>> GetAllAsync(int userId)
        {
            var isAdmin = await _context.UserRoles.AnyAsync(x =>x.UserId == userId &&x.RoleId == 1);
            var isTechnicalApprover = await _context.UserRoles.AnyAsync(x =>x.UserId == userId &&x.RoleId == 7);
            var query = _context.SalesOrders.AsNoTracking();
            if (!isAdmin && isTechnicalApprover)
            {
                query = query.Where(x =>x.Status == "TEKNIK_ONAYDA");
            }
            return await query.OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<SalesOrder?> GetByIdAsync(long id)
        {
            return await _context.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<SalesOrder?> UpdateAsync(long id,SalesOrder salesOrder,int userId)
        {
            var existingOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (existingOrder == null) return null;

            // Nihai siparişe geçmiş kayıt burada değiştirilemez.
            if (existingOrder.Status == "NIHAI_ONAYLANDI" || existingOrder.Status == "NETSIS_AKTARILDI")
            {
                throw new InvalidOperationException("Nihai onaylanmış veya Netsis'e aktarılmış sipariş değiştirilemez.");
            }

            existingOrder.SalesRepresentativeUserId =salesOrder.SalesRepresentativeUserId;
            existingOrder.OrderNumber =salesOrder.OrderNumber;
            existingOrder.OrderYear =salesOrder.OrderYear;
            existingOrder.SalesType =salesOrder.SalesType;
            existingOrder.OrderType =salesOrder.OrderType;
            existingOrder.CustomerCode =salesOrder.CustomerCode;
            existingOrder.CustomerName =salesOrder.CustomerName;
            existingOrder.DeliveryMethod =salesOrder.DeliveryMethod;
            existingOrder.IncotermId =salesOrder.IncotermId;
            existingOrder.DueDate =salesOrder.DueDate;
            existingOrder.CurrencyCode =salesOrder.CurrencyCode;
            existingOrder.UpdatedAt =DateTime.Now;
            existingOrder.UpdatedBy =userId;
            await _context.SaveChangesAsync();
            return existingOrder;
        }
        private async Task<string> CreateSystemOrderNumberAsync(string salesType,string orderNumber,int orderYear)
        {
            var salesTypeParameter = new SqlParameter("@SIPTUR", SqlDbType.NVarChar, 25) { Value = salesType };
            var orderNumberParameter = new SqlParameter("@SIPNO", SqlDbType.NVarChar, 25) { Value = orderNumber };
            var yearParameter = new SqlParameter("@YIL", SqlDbType.Int) { Value = orderYear };

            var result = await _context.Database.SqlQueryRaw<SystemOrderNumberResult>(
                    @"EXEC dbo.granitSP_SipNoCreate @SIPTUR, @SIPNO, @YIL",
                    salesTypeParameter,
                    orderNumberParameter,
                    yearParameter)
                .ToListAsync();
            var systemOrderNumber =result.FirstOrDefault()?.SystemOrderNumber;
            if (string.IsNullOrWhiteSpace(systemOrderNumber))
            {
                throw new Exception("Sistem sipariş numarası oluşturulamadı.");
            }
            return systemOrderNumber;
        }
        private async Task<bool> IsSystemOrderNumberUsedAsync(string systemOrderNumber)
        {
            var parameter = new SqlParameter("@FATIRS_NO", SqlDbType.NVarChar, 50) {Value = systemOrderNumber};
            var result = await _context.Database.SqlQueryRaw<int>(
                    @"SELECT 
                        CASE
                            WHEN EXISTS (SELECT 1 FROM GRANITMTL2026..TBLSIPAMAS WHERE FATIRS_NO = @FATIRS_NO)
                            OR 
                            EXISTS (SELECT 1 FROM dbo.SalesOrders WHERE SystemOrderNumber = @FATIRS_NO)
                            THEN 1
                            ELSE 0
                        END AS [Value]",
                    parameter)
                .FirstAsync();
            return result == 1;
        }
        public async Task<List<SalesOrderLineList>> GetLinesAsync(long salesOrderId)
        {
            var sql = @"
        SELECT  
            SOL.LineNumber + 1 AS Line, 
            SOL.Id, 
            SOL.ProductGroupId,
            SOL.CatalogCode, 
            SOL.ProductName, 

            (
                SELECT TOP 1 AYAK_RAL_DETAY 
                FROM GRANIT_TBL_CK_URUN_DETAY_RENK 
                WHERE AYAK_RAL = SOLCK.FootRal 
            )
            + '/' + 
            (
                SELECT TOP 1 AYAK_RAL_DETAY 
                FROM GRANIT_TBL_CK_URUN_DETAY_RENK 
                WHERE AYAK_RAL = SOLCK.BodyWingRal 
            ) AS Renk, 

            (
                SELECT RENK 
                FROM GRANIT_TBL_PLASTIK_RENK 
                WHERE NO = SOLCK.PlasticColor1No 
            )
            + '/' + 
            (
                SELECT RENK 
                FROM GRANIT_TBL_PLASTIK_RENK 
                WHERE NO = SOLCK.PlasticColor2No 
            ) AS Plastik, 

            SOL.Quantity,

            -- SATIR ÜZERİNDEKİ ANA MONTAJ KODLARI
            SOL.MainAssemblyCode,
            SOL.ManualAssemblyCode,
            SOL.MainAssemblyControl,

            -- CONFIGURATION ÜZERİNDEKİ ANA MONTAJ KODLARI
            SOLCK.MainAssemblyCode AS ConfigurationMainAssemblyCode,
            SOLCK.ManualAssemblyCode AS ConfigurationManualAssemblyCode,
            SOL.ShrinkliKod

        FROM SalesOrders SO 

        INNER JOIN SalesOrderLines SOL 
            ON SO.Id = SOL.SalesOrderId 

        INNER JOIN SalesOrderLineCKConfigurations SOLCK 
            ON SOL.Id = SOLCK.SalesOrderLineId 

        WHERE SO.Id = @SalesOrderId
          AND SOL.DeletedFlag = 0

        ORDER BY SOL.LineNumber; 
    ";

            return await _context.Database
                .SqlQueryRaw<SalesOrderLineList>(
                    sql,
                    new SqlParameter("@SalesOrderId", salesOrderId)
                )
                .ToListAsync();
        }

        // ANA MONTAJ ERP KONTROLÜ
        public async Task<SalesOrderErpCheckResult> CheckErpAsync(long salesOrderId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId);

                if (salesOrder == null)
                {
                    throw new Exception($"Satış siparişi bulunamadı. Id: {salesOrderId}");
                }

                var lines = await _context.SalesOrderLines
                        .Where(x => x.SalesOrderId == salesOrderId && !x.DeletedFlag)
                        .Include(x => x.CKConfiguration)
                        .OrderBy(x => x.LineNumber)
                        .ToListAsync();

                if (lines.Count == 0) { throw new Exception("Siparişte kontrol edilecek aktif satır bulunamadı."); }
                var result = new SalesOrderErpCheckResult { TotalLines = lines.Count };

                foreach (var line in lines)
                {
                    if (line.ProductGroupId != "1")
                    {
                        throw new Exception(
                            $"ERP kontrolü şu anda sadece CK ürün grubu için desteklenmektedir. " +
                            $"Satır: {line.LineNumber}, " +
                            $"ProductGroupId: {line.ProductGroupId}");
                    }

                    var configuration = line.CKConfiguration;
                    if (configuration == null)
                    {
                        throw new Exception($"Satır {line.LineNumber} için CK ürün konfigürasyonu bulunamadı.");
                    }

                    if (string.IsNullOrWhiteSpace(configuration.CatalogCode))
                    {
                        throw new Exception($"Satır {line.LineNumber} için katalog kodu bulunamadı.");
                    }

                    if (string.IsNullOrWhiteSpace(configuration.FootRal))
                    {
                        throw new Exception($"Satır {line.LineNumber} için ayak RAL bilgisi bulunamadı.");
                    }

                    if (string.IsNullOrWhiteSpace(configuration.BodyWingRal))
                    {
                        throw new Exception($"Satır {line.LineNumber} için gövde/kanat RAL bilgisi bulunamadı.");
                    }

                    if (string.IsNullOrWhiteSpace(configuration.PlasticColor1No))
                    {
                        throw new Exception($"Satır {line.LineNumber} için plastik renk 1 bulunamadı.");
                    }

                    if (string.IsNullOrWhiteSpace(configuration.PlasticColor2No))
                    {
                        throw new Exception($"Satır {line.LineNumber} için plastik renk 2 bulunamadı.");
                    }

                    var productGroupId = configuration.ProductGroupId ?? line.ProductGroupId;

                    var mainAssemblyCode =
                        $"2{productGroupId}90." +
                        $"{configuration.CatalogCode}." +
                        $"{configuration.FootRal}." +
                        $"{configuration.BodyWingRal}." +
                        $"{configuration.PlasticColor1No}." +
                        $"{configuration.PlasticColor2No}";

                    var manualAssemblyCode =
                        $"2{productGroupId}80." +
                        $"{configuration.CatalogCode}." +
                        $"{configuration.FootRal}." +
                        $"{configuration.BodyWingRal}." +
                        $"{configuration.PlasticColor1No}." +
                        $"{configuration.PlasticColor2No}";


                    var mainExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == mainAssemblyCode);
                    var manualExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == manualAssemblyCode);

                    configuration.MainAssemblyCode = mainAssemblyCode;
                    configuration.ManualAssemblyCode = manualAssemblyCode;
                    configuration.MainAssemblyCodeExists = mainExists;
                    configuration.ManualAssemblyCodeExists = manualExists;
                    configuration.UpdatedAt = DateTime.Now;
                    configuration.UpdatedBy = _userContext.UserId;

                    // SALES ORDER LINE GÜNCELLE
                    line.MainAssemblyCode = mainAssemblyCode;
                    line.ManualAssemblyCode = manualAssemblyCode;
                    line.MainAssemblyControl = mainExists || manualExists;
                    line.UpdatedAt = DateTime.Now;
                    line.UpdatedBy = _userContext.UserId;

                    // SONUÇ
                    if (line.MainAssemblyControl == true)
                    {
                        result.AvailableLines++;
                    }
                    else
                    {
                        result.MissingLines.Add(
                            new SalesOrderErpMissingLine
                            {
                                LineNumber = line.LineNumber,
                                ProductName = line.ProductName,
                                MainAssemblyCode = mainAssemblyCode,
                                ManualAssemblyCode = manualAssemblyCode
                            });
                    }
                }

                result.AllAvailable = result.MissingLines.Count == 0;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> SendForApprovalAsync(long salesOrderId, int userId)
        {
            var salesOrder = await _context.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (salesOrder == null)
                throw new Exception("Satış siparişi bulunamadı.");

            if (salesOrder.CreatedBy != userId)
                throw new Exception(
                    "Bu siparişi sadece oluşturan kullanıcı onaya gönderebilir.");

            if (salesOrder.Status != "TASLAK")
                throw new Exception(
                    $"Sipariş mevcut durumda onaya gönderilemez. Durum: {salesOrder.Status}");

            // ERP kontrolü
            var erpResult = await CheckErpAsync(salesOrderId);

            if (!erpResult.AllAvailable)
                throw new Exception(
                    "ERP kontrolü başarısız. Eksik montaj kodları bulunmaktadır.");

            // SATIS_SIPARIS ProcessType
            var processType = await _context.ProcessTypes
                .FirstOrDefaultAsync(x => x.Code == "SATIS_SIPARIS");

            if (processType == null)
                throw new Exception(
                    "SATIS_SIPARIS ProcessType tanımı bulunamadı.");

            // Workflow ProcessRequest
            var processRequest = new ProcessRequest
            {
                ProcessTypeId = processType.Id,
                CreatedBy = userId,
                Title = $"Satış Siparişi - {salesOrder.SystemOrderNumber}",
                EntityId = (int)salesOrder.Id
            };

            // Workflow'u başlat ve onaya gönder
            var processRequestId =
                await _workflowService.StartAndSubmitAsync(
                    processRequest,
                    "Satış siparişi teknik onaya gönderildi.");

            // Sipariş - Workflow bağlantısı
            salesOrder.ProcessRequestId = processRequestId;
            salesOrder.Status = "TEKNIK_ONAYDA";
            salesOrder.UpdatedAt = DateTime.Now;
            salesOrder.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return processRequestId;
        }

        public async Task CompleteTechnicalAsync(long salesOrderId, int userId)
        {
            var salesOrder = await _context.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (salesOrder == null)
                throw new Exception("Satış siparişi bulunamadı.");

            if (salesOrder.Status != "TEKNIK_ONAYDA")
                throw new Exception(
                    $"Sipariş teknik aşamadan geçirilemez. Durum: {salesOrder.Status}");

            var lines = await _context.SalesOrderLines
                .Where(x =>
                    x.SalesOrderId == salesOrderId &&
                    !x.DeletedFlag)
                .ToListAsync();

            if (!lines.Any())
                throw new Exception("Sipariş kalemi bulunamadı.");

            var incompleteLines = lines
                .Where(x => string.IsNullOrWhiteSpace(x.ShrinkliKod))
                .ToList();

            if (incompleteLines.Any())
            {
                throw new Exception(
                    "Teknik işlemleri tamamlanmamış sipariş kalemleri bulunmaktadır.");
            }

            salesOrder.Status = "PAKETLEME";
            salesOrder.UpdatedAt = DateTime.Now;
            salesOrder.UpdatedBy = userId;

            await _context.SaveChangesAsync();
        }

    }
}