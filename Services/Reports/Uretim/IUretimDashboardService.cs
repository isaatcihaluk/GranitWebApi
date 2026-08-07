using GranitWebApi.Models.Reports;
using GranitWebApi.Models.Reports.Uretim;

namespace GranitWebApi.Services.Reports.Uretim
{
    public interface IUretimDashboardService
    {
        Task<OeeSummaryResult> GetOeeSummaryAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<OeeTrendResult>> GetOeeTrendAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<OeeMachineResult>> GetMachineOeeAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<DowntimeSummaryResult> GetDowntimeSummaryAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<DowntimeMachineResult>> GetDowntimeMachineAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<DowntimeReasonResult>> GetDowntimeReasonAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<DowntimeTrendResult>> GetDowntimeTrendAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<ProductionSummaryResult> GetProductionSummaryAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<ProductionTrendResult>> GetProductionTrendAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<ProductionMachineResult>> GetProductionMachineAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<QualitySummaryResult> GetQualitySummaryAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<QualityTrendResult>> GetQualityTrendAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<QualityMachineResult>> GetQualityMachineAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<QualityReasonResult>> GetQualityReasonAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<DashboardResult> GetDashboardAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<string>> GetMachineGroupsAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
        Task<List<string>> GetMachinesAsync(UretimDashboardFilter filter,CancellationToken cancellationToken);
    }
}