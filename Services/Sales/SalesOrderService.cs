using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.Sales;
using GranitWebApi.Services.Email.Sales;
using GranitWebApi.Services.Sales;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly AppDbContext _context;
        private readonly ISalesOrderService _;
        private readonly IWorkflowService _workflowService;
        private readonly ErpDbContext _erpContext;
        private readonly UserContext _userContext;
        private readonly ISalesOrderMailService _salesOrderMailService;
        public SalesOrderService(AppDbContext context, UserContext userContext, 
            ErpDbContext erpContext, IWorkflowService workflowService, ISalesOrderMailService salesOrderMailService)
        {
            _context = context;
            _userContext = userContext;
            _erpContext = erpContext;
            _workflowService = workflowService;
            _salesOrderMailService = salesOrderMailService;
        }
        private class SystemOrderNumberResult
        {
            public string SystemOrderNumber { get; set; } = null!;
        }
        private class ShrinkRecipeSqlResult
        {
            public int Basarili { get; set; }
            public int Hata { get; set; }
            public int NetsisReceteVar { get; set; }
            public string? ShrinkliKod { get; set; }
            public string? Aciklama { get; set; }
        }
        public class SalesOrderPackageReceteSqlResult
        {
            public bool Basarili { get; set; }
            public bool Hata { get; set; }
            public bool NetsisReceteVar { get; set; }
            public long SalesOrderPackageId { get; set; }
            public string? PaketKod { get; set; }
            public int? NetsisStatus { get; set; }
            public string? NetsisResponse { get; set; }
            public string? Aciklama { get; set; }
        }
        public class SalesOrderNetsisTransferSqlResult
        {
            public int Basarili { get; set; }
            public int Hata { get; set; }
            public string? NetsisSiparisNo { get; set; }
            public int? NetsisStatus { get; set; }
            public string? NetsisResponse { get; set; }
            public string? ErrorDesc { get; set; }
            public string? Aciklama { get; set; }
        }
        public async Task<SalesOrder> CreateAsync(SalesOrder salesOrder, int userId)
        {
            // Sipariş yılı girilmemişse mevcut yılı kullan
            if (salesOrder.OrderYear <= 0) { salesOrder.OrderYear = DateTime.Now.Year; }
            salesOrder.CreatedBy = userId;
            salesOrder.CreatedAt = DateTime.Now;
            salesOrder.Status = "TASLAK";

            salesOrder.SystemOrderNumber = await CreateSystemOrderNumberAsync(salesOrder.OrderType, salesOrder.OrderNumber, salesOrder.OrderYear);
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
            var isAdmin = await _context.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == 1);
            var isTechnicalApprover = await _context.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == 7);
            var query = _context.SalesOrders.AsNoTracking();
            if (!isAdmin && isTechnicalApprover)
            {
                query = query.Where(x => x.Status == "TEKNIK_ONAYDA");
            }
            return await query.OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<SalesOrder?> GetByIdAsync(long id)
        {
            return await _context.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<SalesOrder?> UpdateAsync(long id, SalesOrder salesOrder, int userId)
        {
            var existingOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (existingOrder == null) return null;

            // Nihai siparişe geçmiş kayıt burada değiştirilemez.
            if (existingOrder.Status == "NIHAI_ONAYLANDI" || existingOrder.Status == "NETSIS_AKTARILDI")
            {
                throw new InvalidOperationException("Nihai onaylanmış veya Netsis'e aktarılmış sipariş değiştirilemez.");
            }

            existingOrder.SalesRepresentativeUserId = salesOrder.SalesRepresentativeUserId;
            existingOrder.OrderNumber = salesOrder.OrderNumber;
            existingOrder.OrderYear = salesOrder.OrderYear;
            existingOrder.SalesType = salesOrder.SalesType;
            existingOrder.OrderType = salesOrder.OrderType;
            existingOrder.CustomerCode = salesOrder.CustomerCode;
            existingOrder.CustomerName = salesOrder.CustomerName;
            existingOrder.DeliveryMethod = salesOrder.DeliveryMethod;
            existingOrder.IncotermId = salesOrder.IncotermId;
            existingOrder.DueDate = salesOrder.DueDate;
            existingOrder.CurrencyCode = salesOrder.CurrencyCode;
            existingOrder.UpdatedAt = DateTime.Now;
            existingOrder.UpdatedBy = userId;
            await _context.SaveChangesAsync();
            return existingOrder;
        }
        private async Task<string> CreateSystemOrderNumberAsync(string salesType, string orderNumber, int orderYear)
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
            var systemOrderNumber = result.FirstOrDefault()?.SystemOrderNumber;
            if (string.IsNullOrWhiteSpace(systemOrderNumber))
            {
                throw new Exception("Sistem sipariş numarası oluşturulamadı.");
            }
            return systemOrderNumber;
        }
        private async Task<bool> IsSystemOrderNumberUsedAsync(string systemOrderNumber)
        {
            var parameter = new SqlParameter("@FATIRS_NO", SqlDbType.NVarChar, 50) { Value = systemOrderNumber };
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

                    CASE
                        WHEN SOL.ProductGroupId = '1' THEN
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
                            )

                        WHEN SOL.ProductGroupId = '2' THEN
                            ISNULL(SOLUM.BodyIroningColor, '')
                            + '/' +
                            ISNULL(SOLUM.FootColor, '')

                        ELSE NULL
                    END AS Renk,

                    CASE
                        WHEN SOL.ProductGroupId = '1' THEN
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
                            )

                        WHEN SOL.ProductGroupId = '2' THEN
                            SOLUM.PlasticCombinationDescription

                        ELSE NULL
                    END AS Plastik,

                    SOL.Quantity,

                    -- SATIR ÜZERİNDEKİ ANA MONTAJ KODLARI
                    SOL.MainAssemblyCode,
                    SOL.ManualAssemblyCode,
                    SOL.MainAssemblyControl,

                    -- CK CONFIGURATION
                    SOLCK.MainAssemblyCode AS ConfigurationMainAssemblyCode,
                    SOLCK.ManualAssemblyCode AS ConfigurationManualAssemblyCode,
                    SOL.ShrinkliKod

                FROM SalesOrders SO
                INNER JOIN SalesOrderLines SOL ON SO.Id = SOL.SalesOrderId
                LEFT JOIN SalesOrderLineCKConfigurations SOLCK ON SOL.Id = SOLCK.SalesOrderLineId
                LEFT JOIN SalesOrderLineUMConfigurations SOLUM ON SOL.Id = SOLUM.SalesOrderLineId
                WHERE SO.Id = @SalesOrderId AND SOL.DeletedFlag = 0
                ORDER BY SOL.LineNumber;
                ";

            return await _context.Database.SqlQueryRaw<SalesOrderLineList>(sql,new SqlParameter("@SalesOrderId", salesOrderId)).ToListAsync();
        }

        // ANA MONTAJ ERP KONTROLÜ
        public async Task<SalesOrderErpCheckResult> CheckErpAsync(long salesOrderId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var salesOrder =await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId);
                if (salesOrder == null)
                {
                    throw new Exception($"Satış siparişi bulunamadı. Id: {salesOrderId}");
                }

                var lines = await _context.SalesOrderLines
                        .Where(x => x.SalesOrderId == salesOrderId && !x.DeletedFlag)
                        .Include(x => x.CKConfiguration).Include(x => x.UMConfiguration)
                        .OrderBy(x => x.LineNumber).ToListAsync();

                if (lines.Count == 0)
                {
                    throw new Exception("Siparişte kontrol edilecek aktif satır bulunamadı.");
                }

                var result = new SalesOrderErpCheckResult {TotalLines = lines.Count};

                foreach (var line in lines)
                {
                    string mainAssemblyCode;
                    string manualAssemblyCode;
                    bool mainExists;
                    bool manualExists;

                    // CK
                    if (line.ProductGroupId == "1")
                    {
                        var configuration = line.CKConfiguration;
                        if (configuration == null)
                        {
                            throw new Exception($"Satır {line.LineNumber} için " +
                                $"CK ürün konfigürasyonu bulunamadı.");
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

                        mainAssemblyCode =
                            $"2{productGroupId}90." +
                            $"{configuration.CatalogCode}." +
                            $"{configuration.FootRal}." +
                            $"{configuration.BodyWingRal}." +
                            $"{configuration.PlasticColor1No}." +
                            $"{configuration.PlasticColor2No}";

                        manualAssemblyCode =
                            $"2{productGroupId}80." +
                            $"{configuration.CatalogCode}." +
                            $"{configuration.FootRal}." +
                            $"{configuration.BodyWingRal}." +
                            $"{configuration.PlasticColor1No}." +
                            $"{configuration.PlasticColor2No}";

                        mainExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == mainAssemblyCode);
                        manualExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == manualAssemblyCode);

                        configuration.MainAssemblyCode = mainAssemblyCode;
                        configuration.ManualAssemblyCode =manualAssemblyCode;
                        configuration.MainAssemblyCodeExists =mainExists;
                        configuration.ManualAssemblyCodeExists =manualExists;
                        configuration.UpdatedAt =DateTime.Now;
                        configuration.UpdatedBy =_userContext.UserId;
                    }

                    // UM
                    else if (line.ProductGroupId == "2")
                    {
                        var configuration = line.UMConfiguration;
                        if (configuration == null)
                        {
                            throw new Exception(
                                $"Satır {line.LineNumber} için " +
                                $"UM ürün konfigürasyonu bulunamadı.");
                        }

                        if (string.IsNullOrWhiteSpace(line.CatalogCode))
                        {
                            throw new Exception($"Satır {line.LineNumber} için katalog kodu bulunamadı.");
                        }

                        if (string.IsNullOrWhiteSpace(configuration.ProductGroupId))
                        {
                            throw new Exception($"Satır {line.LineNumber} için UM ProductGroupId bulunamadı.");
                        }

                        if (string.IsNullOrWhiteSpace(configuration.BodyIroningRal))
                        {
                            throw new Exception(
                                $"Satır {line.LineNumber} için " +
                                $"UM gövde/ütülük RAL bilgisi bulunamadı.");
                        }

                        if (string.IsNullOrWhiteSpace(configuration.FootColor))
                        {
                            throw new Exception(
                                $"Satır {line.LineNumber} için " +
                                $"UM ayak rengi bulunamadı.");
                        }

                        if (string.IsNullOrWhiteSpace(configuration.PlasticCombinationNo))
                        {
                            throw new Exception(
                                $"Satır {line.LineNumber} için " +
                                $"UM plastik kombinasyonu bulunamadı.");
                        }

                        var productGroupId = configuration.ProductGroupId;

                        // 2290.11010.9016.SIYAH.03
                        mainAssemblyCode =
                            $"2{productGroupId}90." +
                            $"{line.CatalogCode}." +
                            $"{configuration.BodyIroningRal}." +
                            $"{configuration.FootRal}." +
                            $"{configuration.PlasticCombinationNo}";

                        // 2280.11010.9016.SIYAH.03
                        manualAssemblyCode =
                            $"2{productGroupId}80." +
                            $"{line.CatalogCode}." +
                            $"{configuration.BodyIroningRal}." +
                            $"{configuration.FootRal}." +
                            $"{configuration.PlasticCombinationNo}";

                        mainExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == mainAssemblyCode);
                        manualExists =await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x =>x.STOK_KODU == manualAssemblyCode);
                        configuration.MainAssemblyCode =mainAssemblyCode;
                        configuration.ManualAssemblyCode =manualAssemblyCode;
                        configuration.MainAssemblyCodeExists =mainExists;
                        configuration.ManualAssemblyCodeExists =manualExists;
                        configuration.UpdatedAt =DateTime.Now;
                        configuration.UpdatedBy =_userContext.UserId;
                    }

                    //YK
                    else if (line.ProductGroupId == "5")
                    {
                        if (string.IsNullOrWhiteSpace(line.CatalogCode))
                        {
                            throw new Exception($"Satır {line.LineNumber} için YK katalog kodu bulunamadı.");
                        }

                        // YK'da katalog kodunun TBLSTSABIT'te bulunması yeterlidir.
                        mainAssemblyCode = line.CatalogCode;
                        manualAssemblyCode = line.CatalogCode;

                        mainExists = await _erpContext.TBLSTSABIT.AsNoTracking().AnyAsync(x => x.STOK_KODU == line.CatalogCode);
                        manualExists = mainExists;
                    }

                    // DİĞER ÜRÜN GRUPLARI
                    else
                    {
                        throw new Exception(
                            $"ERP kontrolü desteklenmeyen ürün grubu içeriyor. " +
                            $"Satır: {line.LineNumber}, " +
                            $"ProductGroupId: {line.ProductGroupId}");
                    }

                    // SALES ORDER LINE GÜNCELLE
                    line.MainAssemblyCode =mainAssemblyCode;
                    line.ManualAssemblyCode =manualAssemblyCode;
                    line.MainAssemblyControl =mainExists || manualExists;
                    line.UpdatedAt =DateTime.Now;
                    line.UpdatedBy =_userContext.UserId;

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
            var salesOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (salesOrder == null)
                throw new Exception("Satış siparişi bulunamadı.");

            if (salesOrder.CreatedBy != userId)
                throw new Exception("Bu siparişi sadece oluşturan kullanıcı onaya gönderebilir.");

            if (salesOrder.Status != "TASLAK")
                throw new Exception($"Sipariş mevcut durumda onaya gönderilemez. Durum: {salesOrder.Status}");

            // ERP kontrolü
            var erpResult = await CheckErpAsync(salesOrderId);

            if (!erpResult.AllAvailable)
                throw new Exception("ERP kontrolü başarısız. Eksik montaj kodları bulunmaktadır.");

            // SATIS_SIPARIS ProcessType
            var processType = await _context.ProcessTypes
                .FirstOrDefaultAsync(x => x.Code == "SATIS_SIPARIS");

            if (processType == null)
                throw new Exception("SATIS_SIPARIS ProcessType tanımı bulunamadı.");

            // Workflow ProcessRequest
            var processRequest = new ProcessRequest
            {
                ProcessTypeId = processType.Id,
                CreatedBy = userId,
                Title = $"Satış Siparişi - {salesOrder.SystemOrderNumber}",
                EntityId = (int)salesOrder.Id
            };

            // Workflow'u başlat ve onaya gönder
            var processRequestId = await _workflowService.StartAndSubmitAsync(processRequest,"Satış siparişi teknik onaya gönderildi.");

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
        public async Task<SalesOrderShrinkRecipePrepareResult> PrepareShrinkRecipesAsync(long salesOrderId)
        {
            var salesOrder = await _context.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (salesOrder == null)
            {
                return new SalesOrderShrinkRecipePrepareResult
                {
                    Basarili = false,
                    Hata = true,
                    Aciklama = $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                };
            }

            var lines = await _context.SalesOrderLines.AsNoTracking()
                .Where(x => x.SalesOrderId == salesOrderId && !x.DeletedFlag)
                .OrderBy(x => x.LineNumber).ToListAsync();

            if (!lines.Any())
            {
                return new SalesOrderShrinkRecipePrepareResult
                {
                    Basarili = false,
                    Hata = true,
                    Aciklama = "Siparişte kontrol edilecek aktif satır bulunamadı."
                };
            }

            foreach (var line in lines)
            {
                try
                {
                    if (line.ProductGroupId == "5")
                    {
                        continue;
                    }
                    string? procedureName = line.ProductGroupId switch
                    {
                        "1" => "dbo.granitSP_SalesOrderCKShrinklenmisReceteOlusturma",
                        "2" => "dbo.granitSP_SalesOrderUMShrinklenmisReceteOlusturma",
                        _ => null
                    };

                    if (procedureName == null)
                    {
                        return new SalesOrderShrinkRecipePrepareResult
                        {
                            Basarili = false,
                            Hata = true,
                            SalesOrderLineId = line.Id,
                            LineNumber = line.LineNumber + 1,
                            ProductName = line.ProductName,
                            ShrinkliKod = line.ShrinkliKod,
                            Aciklama =
                                $"Ürün grubu için shrinkli reçete SP'si tanımlı değil. " +
                                $"ProductGroupId: {line.ProductGroupId}"
                        };
                    }

                    var sql = $@"
                EXEC {procedureName}
                    @SalesOrderLineId";

                    var results = await _erpContext.Database
                        .SqlQueryRaw<ShrinkRecipeSqlResult>(
                            sql,
                            new SqlParameter(
                                "@SalesOrderLineId",
                                SqlDbType.BigInt)
                            {
                                Value = line.Id
                            })
                        .ToListAsync();

                    var result = results.FirstOrDefault();

                    if (result == null)
                    {
                        return new SalesOrderShrinkRecipePrepareResult
                        {
                            Basarili = false,
                            Hata = true,
                            SalesOrderLineId = line.Id,
                            LineNumber = line.LineNumber + 1,
                            ProductName = line.ProductName,
                            ShrinkliKod = line.ShrinkliKod,
                            Aciklama =
                                "Shrinkli reçete SP'sinden sonuç alınamadı."
                        };
                    }

                    if (result.Basarili == 0 || result.Hata == 1)
                    {
                        return new SalesOrderShrinkRecipePrepareResult
                        {
                            Basarili = false,
                            Hata = true,
                            SalesOrderLineId = line.Id,
                            LineNumber = line.LineNumber + 1,
                            ProductName = line.ProductName,
                            ShrinkliKod = result.ShrinkliKod,
                            NetsisReceteVar = result.NetsisReceteVar == 1,
                            Aciklama = result.Aciklama
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new SalesOrderShrinkRecipePrepareResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderLineId = line.Id,
                        LineNumber = line.LineNumber + 1,
                        ProductName = line.ProductName,
                        ShrinkliKod = line.ShrinkliKod,
                        Aciklama =
                            $"Shrinkli reçete oluşturulurken hata oluştu: {ex.Message}"
                    };
                }
            }

            return new SalesOrderShrinkRecipePrepareResult
            {
                Basarili = true,
                Hata = false,
                NetsisReceteVar = true,
                Aciklama =
                    "Tüm shrinkli reçeteler başarıyla kontrol edildi ve hazırlandı."
            };
        }
        public async Task<SalesOrderPackageRecetePrepareResult> PreparePackageRecipesAsync(long salesOrderId)
        {
            var salesOrder = await _context.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesOrderId);

            if (salesOrder == null)
            {
                return new SalesOrderPackageRecetePrepareResult
                {
                    Basarili = false,
                    Hata = true,
                    Aciklama = $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                };
            }

            var packages = await _context.SalesOrderPackages
                .AsNoTracking()
                .Where(x =>
                    x.SalesOrderId == salesOrderId &&
                    x.Status == "TASLAK")
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (!packages.Any())
            {
                return new SalesOrderPackageRecetePrepareResult
                {
                    Basarili = false,
                    Hata = true,
                    Aciklama = "Siparişte kontrol edilecek paket bulunamadı."
                };
            }

            foreach (var package in packages)
            {
                try
                {
                    var results = await _erpContext.Database
                        .SqlQueryRaw<SalesOrderPackageReceteSqlResult>(
                            @"EXEC dbo.granitSP_SalesOrderPackageReceteOlusturma
                                @SalesOrderPackageId",
                            new SqlParameter(
                                "@SalesOrderPackageId",
                                SqlDbType.BigInt)
                            {
                                Value = package.Id
                            })
                        .ToListAsync();

                    var result = results.FirstOrDefault();

                    if (result == null)
                    {
                        return new SalesOrderPackageRecetePrepareResult
                        {
                            Basarili = false,
                            Hata = true,
                            SalesOrderPackageId = package.Id,
                            PaketSira = Convert.ToInt32(package.Id),
                            PaketKod = package.NetsisPaketKodu,
                            Aciklama = "Paket reçete SP'sinden sonuç alınamadı."
                        };
                    }

                    // SP BIT döndürdüğü için bool olarak kontrol ediyoruz.
                    if (!result.Basarili || result.Hata)
                    {
                        return new SalesOrderPackageRecetePrepareResult
                        {
                            Basarili = false,
                            Hata = true,
                            NetsisReceteVar = result.NetsisReceteVar,
                            SalesOrderPackageId = package.Id,
                            PaketSira = Convert.ToInt32(package.Id),
                            PaketKod = result.PaketKod ?? package.NetsisPaketKodu,
                            NetsisStatus = result.NetsisStatus,
                            NetsisResponse = result.NetsisResponse,
                            Aciklama = result.Aciklama
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new SalesOrderPackageRecetePrepareResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderPackageId = package.Id,
                        PaketSira = Convert.ToInt32(package.Id),
                        PaketKod = package.NetsisPaketKodu,
                        Aciklama = $"Paket reçetesi oluşturulurken hata oluştu: {ex.Message}"
                    };
                }
            }

            return new SalesOrderPackageRecetePrepareResult
            {
                Basarili = true,
                Hata = false,
                NetsisReceteVar = true,
                Aciklama = "Tüm paket reçeteleri başarıyla kontrol edildi ve hazırlandı."
            };
        }
        public async Task<SalesOrderNetsisTransferResult> TransferToNetsisAsync(long salesOrderId)
        {
            var salesOrder = await _context.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == salesOrderId);
            if (salesOrder == null)
            {
                return new SalesOrderNetsisTransferResult
                {
                    Basarili = false,
                    Hata = true,
                    SalesOrderId = salesOrderId,
                    Aciklama = $"Satış siparişi bulunamadı. Id: {salesOrderId}"
                };
            }

            try
            {
                // 1. GRANITMTL2026
                var granitResults = await _erpContext.Database
                    .SqlQueryRaw<SalesOrderNetsisTransferSqlResult>(
                        @"EXEC dbo.granitSP_SalesOrderNetsisAktar @SalesOrderId",
                        new SqlParameter("@SalesOrderId",SqlDbType.BigInt){Value = salesOrderId})
                    .ToListAsync();

                var granitResult = granitResults.FirstOrDefault();
                if (granitResult == null)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "GRANITMTL2026 Netsis aktarım SP'sinden sonuç alınamadı."
                    };
                }

                if (granitResult.Basarili == 0 || granitResult.Hata == 1)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        NetsisSiparisNo = granitResult.NetsisSiparisNo,
                        NetsisStatus = granitResult.NetsisStatus,
                        NetsisResponse = granitResult.NetsisResponse,
                        ErrorDesc = granitResult.ErrorDesc,
                        Aciklama = $"GRANITMTL2026 aktarımı başarısız: {granitResult.Aciklama}"
                    };
                }

                // 2. EGEKIRAN2026
                var egeResults = await _erpContext.Database
                    .SqlQueryRaw<SalesOrderNetsisTransferSqlResult>(
                        @"EXEC EGEKIRAN2026.dbo.granitSP_SalesOrderNetsisAktar @SalesOrderId",
                        new SqlParameter("@SalesOrderId",SqlDbType.BigInt){Value = salesOrderId})
                    .ToListAsync();

                var egeResult = egeResults.FirstOrDefault();
                if (egeResult == null)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "EGEKIRAN2026 Netsis aktarım SP'sinden sonuç alınamadı."
                    };
                }

                if (egeResult.Basarili == 0 || egeResult.Hata == 1)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        NetsisSiparisNo = egeResult.NetsisSiparisNo,
                        NetsisStatus = egeResult.NetsisStatus,
                        NetsisResponse = egeResult.NetsisResponse,
                        ErrorDesc = egeResult.ErrorDesc,
                        Aciklama = $"EGEKIRAN2026 aktarımı başarısız: {egeResult.Aciklama}"
                    };
                }

                // 3. HER İKİ FİRMA BAŞARILI
                var orderToUpdate = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId);
                if (orderToUpdate == null)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = false,
                        Hata = true,
                        SalesOrderId = salesOrderId,
                        Aciklama = "Netsis aktarımı başarılı ancak satış siparişi güncellemek için bulunamadı."
                    };
                }

                orderToUpdate.Status = "TAMAMLANDI";
                orderToUpdate.NetsisTransferredAt=DateTime.Now;
                orderToUpdate.NetsisOrderNumber = salesOrder.SystemOrderNumber;
                orderToUpdate.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // 4. MAIL
                var mailResult = await _salesOrderMailService.SendSalesOrderMailAsync(salesOrderId);
                if (!mailResult.Basarili)
                {
                    return new SalesOrderNetsisTransferResult
                    {
                        Basarili = true,
                        Hata = false,
                        SalesOrderId = salesOrderId,
                        NetsisSiparisNo =egeResult.NetsisSiparisNo ?? granitResult.NetsisSiparisNo,
                        NetsisStatus = egeResult.NetsisStatus,
                        NetsisResponse = egeResult.NetsisResponse,
                        ErrorDesc = egeResult.ErrorDesc,
                        Aciklama =
                            "Sipariş Netsis'e başarıyla aktarıldı ve TAMAMLANDI durumuna alındı. " +
                            $"Ancak bildirim maili gönderilemedi: {mailResult.Aciklama}"
                    };
                }

                // 5. TAM BAŞARILI
                return new SalesOrderNetsisTransferResult
                {
                    Basarili = true,
                    Hata = false,
                    SalesOrderId = salesOrderId,
                    NetsisSiparisNo =
                        egeResult.NetsisSiparisNo ??
                        granitResult.NetsisSiparisNo,
                    NetsisStatus = egeResult.NetsisStatus,
                    NetsisResponse = egeResult.NetsisResponse,
                    ErrorDesc = egeResult.ErrorDesc,
                    Aciklama =
                        "Sipariş GRANITMTL2026 ve EGEKIRAN2026 sistemlerine başarıyla aktarıldı. " +
                        "Sipariş TAMAMLANDI durumuna alındı ve bildirim maili gönderildi."
                };
            }
            catch (Exception ex)
            {
                return new SalesOrderNetsisTransferResult
                {
                    Basarili = false,
                    Hata = true,
                    SalesOrderId = salesOrderId,
                    Aciklama = $"Sipariş Netsis'e aktarılırken hata oluştu: {ex.Message}"
                };
            }
        }
        public async Task<SalesOrder?> UpdateRevisionHeaderAsync(long id,SalesOrder salesOrder,int userId)
        {
            var existingOrder = await _context.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);

            if (existingOrder == null)
            {
                throw new InvalidOperationException("Satış siparişi bulunamadı.");
            }

            // siparişlerin revizyonu için kullanılabilir.
            if (!existingOrder.NetsisTransferredAt.HasValue)
            {
                throw new InvalidOperationException("Netsis'e aktarılmamış sipariş için revizyon yapılamaz.");
            }

            if (string.IsNullOrWhiteSpace(existingOrder.NetsisOrderNumber))
            {
                throw new InvalidOperationException("Siparişin Netsis sipariş numarası bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(salesOrder.CustomerCode))
            {
                throw new InvalidOperationException("Müşteri kodu boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(salesOrder.CustomerName))
            {
                throw new InvalidOperationException("Müşteri adı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(salesOrder.CurrencyCode))
            {
                throw new InvalidOperationException("Döviz bilgisi boş olamaz.");
            }
            var netsisOrderNumber = existingOrder.SystemOrderNumber;
            var customerCode = salesOrder.CustomerCode.Trim();
            var customerName = salesOrder.CustomerName.Trim();
            int dovizTip = salesOrder.CurrencyCode.ToUpperInvariant() switch
            {"EUR" => 5,"USD" => 4,_ => 0};
            var dueDate = salesOrder.DueDate;
            var incoterm = salesOrder.IncotermId;

            // SADECE EGEKIRAN2026 FİRMASI
            var netsisOrderNumberParameter = new SqlParameter("@NETSISORDERNUMBER",SqlDbType.NVarChar,50){Value = netsisOrderNumber};
            var customerCodeParameter = new SqlParameter("@MUSTERIKODU",SqlDbType.NVarChar,50){Value = customerCode};
            var customerNameParameter = new SqlParameter("@MUSTERIADI",SqlDbType.NVarChar,255){Value = customerName};
            var currencyCodeParameter = new SqlParameter("@DOVIZTIP",SqlDbType.Int)
            { Value = dovizTip };
            var dueDateParameter = new SqlParameter("@TESTAR",SqlDbType.Date){Value = dueDate ?? (object)DBNull.Value};
            var incotermParameter = new SqlParameter("@INCOTERM",SqlDbType.Int){Value = incoterm ?? (object)DBNull.Value};
            var sql = @"
                    -- 1. SİPARİŞ ANA KAYDI
                    UPDATE EGEKIRAN2026.dbo.TBLSIPAMAS
                    SET
                        CARI_KODU = @MUSTERIKODU,
                        DOVIZTIP = @DOVIZTIP,
                        SIPARIS_TEST = @TESTAR,
                        EXPORTTYPE=@INCOTERM
                    WHERE FATIRS_NO = @NETSISORDERNUMBER;

                    -- 2. SİPARİŞ SATIRLARI
                    UPDATE EGEKIRAN2026.dbo.TBLSIPATRA
                    SET
                        STHAR_ACIKLAMA = @MUSTERIKODU,
                        STHAR_CARIKOD = @MUSTERIKODU,
                        STHAR_DOVTIP = @DOVIZTIP,
                        STHAR_TESTAR = @TESTAR,
                        EXPORTTYPE=@INCOTERM
                    WHERE FISNO = @NETSISORDERNUMBER;

                    -- 3. SATIR CARİ BİLGİSİ
                    UPDATE EGEKIRAN2026.dbo.TBLSSATIRAC
                    SET
                        CKOD = @MUSTERIKODU
                    WHERE FATNO = @NETSISORDERNUMBER;

                    -- 4. FATURA EK BİLGİLERİ
                    UPDATE EGEKIRAN2026.dbo.TBLFATUEK
                    SET
                        CKOD = @MUSTERIKODU,
                        ACIK11 = @MUSTERIKODU,
                        ACIK12 = @MUSTERIADI
                    WHERE FATIRSNO = @NETSISORDERNUMBER;
                ";

            var headerUpdated = await _erpContext.Database.ExecuteSqlRawAsync(sql,netsisOrderNumberParameter,customerCodeParameter,customerNameParameter
                ,currencyCodeParameter, dueDateParameter, incotermParameter);

            // TBLSIPAMAS üzerinde kayıt bulunmadıysa
            if (headerUpdated <= 0)
            {
                throw new InvalidOperationException(
                    $"EGEKIRAN2026 TBLSIPAMAS kaydı güncellenemedi. " +
                    $"Netsis Sipariş No: {netsisOrderNumber}");
            }
            // GRANİT ERP ÜST BİLGİ GÜNCELLEME
            existingOrder.SalesRepresentativeUserId =salesOrder.SalesRepresentativeUserId;
            existingOrder.CustomerCode = customerCode;
            existingOrder.CustomerName = customerName;
            existingOrder.DeliveryMethod = salesOrder.DeliveryMethod;
            existingOrder.IncotermId = salesOrder.IncotermId;
            existingOrder.DueDate = salesOrder.DueDate;
            existingOrder.CurrencyCode = salesOrder.CurrencyCode;
            existingOrder.UpdatedAt = DateTime.Now;
            existingOrder.UpdatedBy = userId;
            await _context.SaveChangesAsync();
            return existingOrder;
        }
    }
}