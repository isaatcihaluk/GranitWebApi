using DocumentFormat.OpenXml.InkML;
using GranitWebApi.Data;
using GranitWebApi.Models.Reports;
using GranitWebApi.Models.Reports.Uretim;
using GranitWebApi.Models.Sabitler.Makine;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Reports.Uretim
{
    public class UretimDashboardService : IUretimDashboardService
    {
        private readonly AppDbContext _db;
        public UretimDashboardService(AppDbContext db)
        {
            _db = db;
        }

        #region OEE-Durus
        public async Task<OeeSummaryResult> GetOeeSummaryAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {

            var query = _db.PromanageOee.AsQueryable();

            // ImportDate baz alınacak
            query = query.Where(x => x.ImportDate >= filter.StartDate.Date && x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }


            var result = await query
                .Where(x => x.Oee > 0)
                .GroupBy(x => 1)
                .Select(g => new OeeSummaryResult
                {
                    AverageOee = g.Average(x => x.Oee),
                    AverageAvailability = g.Average(x => x.Availability),
                    AveragePerformance = g.Average(x => x.Performance),
                    AverageQuality = g.Average(x => x.Quality),
                    TotalRecords = g.Count(),
                    MachineCount = g.Select(x => x.MachineName).Distinct().Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? new OeeSummaryResult();
        }
        public async Task<List<OeeTrendResult>> GetOeeTrendAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageOee.AsQueryable();

            query = query.Where(x => x.Oee > 0);

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));


            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            var result = await query
                .GroupBy(x => new
                {
                    Date = x.ImportDate.Date,
                    x.Shift
                })
                .Select(g => new OeeTrendResult
                {
                    Date = g.Key.Date,
                    Shift = g.Key.Shift,
                    AverageOee = g.Average(x => x.Oee),
                    AverageAvailability = g.Average(x => x.Availability),
                    AveragePerformance = g.Average(x => x.Performance),
                    AverageQuality = g.Average(x => x.Quality)
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Shift)
                .ToListAsync(cancellationToken);


            return result;
        }
        public async Task<List<OeeMachineResult>> GetMachineOeeAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageOee.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            var result = await query
                .Where(x => x.Oee > 0)
                .GroupBy(x => x.MachineName)
                .Select(g => new OeeMachineResult
                {
                    MachineName = g.Key,
                    AverageOee = g.Average(x => x.Oee),
                    AverageAvailability = g.Average(x => x.Availability),
                    AveragePerformance = g.Average(x => x.Performance),
                    AverageQuality = g.Average(x => x.Quality)
                })
                .OrderBy(x => x.AverageOee)
                .ToListAsync(cancellationToken);

            return result;
        }
        public async Task<DowntimeSummaryResult> GetDowntimeSummaryAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageStops.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            query = query.Where(x => x.StopName != "Vardiya Bitimi");
            query = query.Where(x => x.StopName != "Belirsiz Duruş" || (x.StopMinute ?? 0) <= 120);


            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return new DowntimeSummaryResult
            {
                TotalMinutes = await query.SumAsync(x => x.StopMinute ?? 0, cancellationToken),
                TotalStops = await query.CountAsync(cancellationToken),
                MachineCount = await query
                    .Where(x => x.MachineName != null)
                    .Select(x => x.MachineName)
                    .Distinct()
                    .CountAsync(cancellationToken)
            };
        }
        public async Task<List<DowntimeReasonResult>> GetDowntimeReasonAsync(UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var query = _db.PromanageStops.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            query = query.Where(x => x.StopName != "Vardiya Bitimi");

            query = query.Where(x =>
                !x.StopName!.StartsWith("Belirsiz Dur") ||
                (x.StopMinute ?? 0) <= 120);

            if (!string.IsNullOrWhiteSpace(filter.Shift))
                query = query.Where(x => x.Shift.ToString() == filter.Shift);

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
                query = query.Where(x => x.MachineName == filter.MachineName);

            // SQL burada bitiyor
            var list = await query.ToListAsync(cancellationToken);

            var linqQuery = query
                .Where(x => x.StopName != null)
                .GroupBy(x => new
                {
                    StopName = x.StopName
                })
                .Select(g => new DowntimeReasonResult
                {
                    StopName = g.Key.StopName,
                    TotalMinutes = g.Sum(x => x.StopMinute ?? 0),
                    StopCount = g.Count()
                });

            Console.WriteLine(linqQuery.ToQueryString());

            // Bundan sonrası tamamen C#
            return list
                .GroupBy(x =>
                {
                    if (x.StopName != null && x.StopName.StartsWith("Belirsiz Dur"))
                    {
                        return (x.StopMinute ?? 0) < 5
                            ? "Belirsiz Duruş (<5 dk)"
                            : "Belirsiz Duruş";
                    }

                    return x.StopName!;
                })
                .Select(g => new DowntimeReasonResult
                {
                    StopName = g.Key,
                    TotalMinutes = g.Sum(x => x.StopMinute ?? 0),
                    StopCount = g.Count()
                })
                .OrderByDescending(x => x.TotalMinutes)
                .Take(10)
                .ToList();
        }
        public async Task<List<DowntimeMachineResult>> GetDowntimeMachineAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageStops.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            query = query.Where(x => x.StopName != "Vardiya Bitimi");
            query = query.Where(x => x.StopName != "Belirsiz Duruş" || (x.StopMinute ?? 0) <= 120);


            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .Where(x => x.MachineName != null)
                .GroupBy(x => x.MachineName)
                .Select(g => new DowntimeMachineResult
                {
                    MachineName = g.Key,
                    TotalMinutes = g.Sum(x => x.StopMinute ?? 0),
                    StopCount = g.Count()
                })
                .OrderByDescending(x => x.TotalMinutes)
                .Take(10)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<DowntimeTrendResult>> GetDowntimeTrendAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageStops.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            query = query.Where(x => x.StopName != "Vardiya Bitimi");
            query = query.Where(x => x.StopName != "Belirsiz Duruş" || (x.StopMinute ?? 0) <= 120);

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .GroupBy(x => new
                {
                    Date = x.ImportDate.Date,
                    x.Shift
                })
                .Select(g => new DowntimeTrendResult
                {
                    Date = g.Key.Date,
                    Shift = g.Key.Shift,
                    TotalMinutes = g.Sum(x => x.StopMinute ?? 0),
                    StopCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Shift)
                .ToListAsync(cancellationToken);
        }
        #endregion

        #region Uretim
        public async Task<ProductionSummaryResult> GetProductionSummaryAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageGunlukUretim.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            var totalProduced = await query.SumAsync(x => x.TotalProduced, cancellationToken);
            var goodProduced = await query.SumAsync(x => x.GoodProduced, cancellationToken);

            return new ProductionSummaryResult
            {
                TotalProduced = totalProduced,
                GoodProduced = goodProduced,
                ScrapAmount = totalProduced - goodProduced,
                QualityRate = totalProduced == 0 ? 0 : (goodProduced / totalProduced) * 100,
                TotalRecords = await query.CountAsync(cancellationToken),
                MachineCount = await query
                    .Select(x => x.MachineName)
                    .Distinct()
                    .CountAsync(cancellationToken)
            };
        }
        public async Task<List<ProductionTrendResult>> GetProductionTrendAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageGunlukUretim.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .GroupBy(x => new
                {
                    Date = x.ImportDate.Date,
                    x.Shift
                })
                .Select(g => new ProductionTrendResult
                {
                    Date = g.Key.Date,
                    Shift = g.Key.Shift,
                    TotalProduced = g.Sum(x => x.TotalProduced),
                    GoodProduced = g.Sum(x => x.GoodProduced),
                    ScrapAmount = g.Sum(x => x.TotalProduced) - g.Sum(x => x.GoodProduced),
                    QualityRate = g.Sum(x => x.TotalProduced) == 0 ? 0 : (g.Sum(x => x.GoodProduced) / g.Sum(x => x.TotalProduced)) * 100
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Shift)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<ProductionMachineResult>> GetProductionMachineAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageGunlukUretim.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .GroupBy(x => new
                {
                    x.MachineName,
                    x.MachineGroup
                })
                .Select(g => new ProductionMachineResult
                {
                    MachineName = g.Key.MachineName,
                    MachineGroup = g.Key.MachineGroup,
                    TotalProduced = g.Sum(x => x.TotalProduced),
                    GoodProduced = g.Sum(x => x.GoodProduced),
                    ScrapAmount =
                        g.Sum(x => x.TotalProduced) -
                        g.Sum(x => x.GoodProduced),

                    QualityRate =
                        g.Sum(x => x.TotalProduced) == 0
                            ? 0
                            : (g.Sum(x => x.GoodProduced) /
                               g.Sum(x => x.TotalProduced)) * 100,

                    RecordCount = g.Count()
                })
                .OrderByDescending(x => x.TotalProduced)
                .ToListAsync(cancellationToken);
        }
        public async Task<QualitySummaryResult> GetQualitySummaryAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageScraps.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return new QualitySummaryResult
            {
                TotalScrap = await query
                    .Where(x => x.RecordType == "Hurda")
                    .SumAsync(x => x.ScrapAmount ?? 0, cancellationToken),

                TotalRework = await query
                    .Where(x => x.RecordType == "Tamir")
                    .SumAsync(x => x.ScrapAmount ?? 0, cancellationToken),

                TotalFire = await query
                    .Where(x => x.RecordType == "Fire")
                    .SumAsync(x => x.ScrapAmount ?? 0, cancellationToken),

                TotalRecords = await query.CountAsync(cancellationToken),
                MachineCount = await query
                    .Select(x => x.MachineName)
                    .Distinct()
                    .CountAsync(cancellationToken)
            };
        }
        public async Task<List<QualityTrendResult>> GetQualityTrendAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageScraps.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .GroupBy(x => new
                {
                    Date = x.ImportDate.Date,
                    x.Shift
                })
                .Select(g => new QualityTrendResult
                {
                    Date = g.Key.Date,
                    Shift = g.Key.Shift,
                    ScrapAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Hurda")
                        .Sum(x => x.ScrapAmount ?? 0),

                    ReworkAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Tamir")
                        .Sum(x => x.ScrapAmount ?? 0),

                    FireAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Fire")
                        .Sum(x => x.ScrapAmount ?? 0),

                    RecordCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Shift)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<QualityMachineResult>> GetQualityMachineAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageScraps.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .GroupBy(x => new
                {
                    x.MachineName,
                    x.MachineGroup
                })
                .Select(g => new QualityMachineResult
                {
                    MachineName = g.Key.MachineName,
                    MachineGroup = g.Key.MachineGroup,
                    ScrapAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Hurda")
                        .Sum(x => x.ScrapAmount ?? 0),

                    ReworkAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Tamir")
                        .Sum(x => x.ScrapAmount ?? 0),

                    FireAmount = g
                        .Where(x => x.RecordType != null &&
                                    x.RecordType.Trim() == "Fire")
                        .Sum(x => x.ScrapAmount ?? 0),

                    TotalRecords = g.Count()
                })
                .OrderByDescending(x => x.ScrapAmount)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<QualityReasonResult>> GetQualityReasonAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageScraps.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Shift))
            {
                query = query.Where(x => x.Shift.ToString() == filter.Shift);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
            {
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);
            }

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
            {
                query = query.Where(x => x.MachineName == filter.MachineName);
            }

            return await query
                .Where(x => x.ScrapName != null)
                .GroupBy(x => new
                {
                    x.ScrapCode,
                    x.ScrapName
                })
                .Select(g => new QualityReasonResult
                {
                    ScrapCode = g.Key.ScrapCode,
                    ScrapName = g.Key.ScrapName,
                    ScrapAmount = g.Sum(x => x.ScrapAmount ?? 0),
                    TotalRecords = g.Count()
                })
                .OrderByDescending(x => x.ScrapAmount)
                .Take(20)
                .ToListAsync(cancellationToken);
        }
        private async Task<List<OperatorProductionResult>> GetOperatorProductionAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            var query = _db.PromanageGunlukUretim.AsQueryable();

            query = query.Where(x =>
                x.ImportDate >= filter.StartDate.Date &&
                x.ImportDate < filter.EndDate.Date.AddDays(1));

            query= query.Where(x => x.TotalProduced!=0);

            if (!string.IsNullOrWhiteSpace(filter.Shift))
                query = query.Where(x => x.Shift == filter.Shift);

            if (!string.IsNullOrWhiteSpace(filter.MachineGroup))
                query = query.Where(x => x.MachineGroup == filter.MachineGroup);

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
                query = query.Where(x => x.MachineName == filter.MachineName);

            return await query
            .Where(x => !string.IsNullOrWhiteSpace(x.Operator))
            .GroupBy(x => new
            {
                x.ReportDate,
                x.Shift,
                x.MachineName,
                x.Operator,
                x.StockName,
                x.TotalProduced
            })
            .Select(g => new OperatorProductionResult
            {
                ReportDate = g.Key.ReportDate,
                Shift = g.Key.Shift,
                MachineName = g.Key.MachineName,
                Operator = g.Key.Operator,
                StockName = g.Key.StockName,

                TotalProduced = g.Sum(x => x.TotalProduced),
                GoodProduced = g.Sum(x => x.GoodProduced),

                QualityRate =g.Sum(x => x.TotalProduced) == 0? 0
                : g.Sum(x => x.GoodProduced) * 100 / g.Sum(x => x.TotalProduced)
            }).OrderByDescending(x => x.TotalProduced).Take(10).ToListAsync(cancellationToken);
        }

        #endregion
        #region Dashboard
        public async Task<DashboardResult> GetDashboardAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {

            return new DashboardResult
            {
                MachineGroups = await _db.Makine
                    .Where(x => x.Bölüm != null)
                    .Select(x => x.Bölüm!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken),


                Machines = await _db.Makine
                    .Where(x => x.MachineName != null)
                    .Select(x => new MachineFilterDto
                    {
                        MachineName = x.MachineName,
                        MachineGroup = x.Bölüm
                    })
                    .OrderBy(x => x.MachineName)
                    .ToListAsync(cancellationToken),

                OeeSummary =
                    await GetOeeSummaryAsync(filter, cancellationToken),

                OeeTrend =
                    await GetOeeTrendAsync(filter, cancellationToken),

                MachineOee =
                    await GetMachineOeeAsync(filter, cancellationToken),


                DowntimeSummary =
                    await GetDowntimeSummaryAsync(filter, cancellationToken),

                DowntimeMachine =
                    await GetDowntimeMachineAsync(filter, cancellationToken),

                DowntimeReason =
                    await GetDowntimeReasonAsync(filter, cancellationToken),

                DowntimeTrend =
                    await GetDowntimeTrendAsync(filter, cancellationToken),


                ProductionSummary =
                    await GetProductionSummaryAsync(filter, cancellationToken),

                ProductionTrend =
                    await GetProductionTrendAsync(filter, cancellationToken),

                ProductionMachine =
                    await GetProductionMachineAsync(filter, cancellationToken),


                QualitySummary =
                    await GetQualitySummaryAsync(filter, cancellationToken),

                QualityTrend =
                    await GetQualityTrendAsync(filter, cancellationToken),

                QualityMachine =
                    await GetQualityMachineAsync(filter, cancellationToken),

                QualityReason =
                    await GetQualityReasonAsync(filter, cancellationToken),

                OperatorProduction =
                    await GetOperatorProductionAsync(filter, cancellationToken)
            };
        }
        #endregion
        #region Makine
        public async Task<List<string>> GetMachineGroupsAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {
            return await _db.Makine
                .Where(x => x.Bölüm != null)
                .Select(x => x.Bölüm!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<string>> GetMachinesAsync(UretimDashboardFilter filter, CancellationToken cancellationToken)
        {

            var query = _db.Makine.AsQueryable();


            if (!string.IsNullOrEmpty(filter.MachineGroup))
            {
                query =
                    query.Where(x =>
                        x.Bölüm == filter.MachineGroup);
            }



            return await query

                .Where(x => x.MachineName != null)

                .Select(x => x.MachineName)

                .Distinct()

                .OrderBy(x => x)

                .ToListAsync(cancellationToken);

        }
        #endregion
    }
}