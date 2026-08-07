using GranitWebApi.Models.Reports.Uretim;
using GranitWebApi.Services.Reports.Uretim;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.Reports
{
    [ApiController]
    [Route("api/reports/uretim")]
    public class UretimDashboardController : ControllerBase
    {
        private readonly IUretimDashboardService _service;


        public UretimDashboardController(
            IUretimDashboardService service)
        {
            _service = service;
        }
        #region OEE-Durus

        [HttpPost("oee-summary")]
        public async Task<IActionResult> GetOeeSummary(UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result =await _service.GetOeeSummaryAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("oee-trend")]
        public async Task<IActionResult> GetOeeTrend(UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result =await _service.GetOeeTrendAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("machine-oee")]
        public async Task<IActionResult> GetMachineOee(UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result =await _service.GetMachineOeeAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("downtime-summary")]
        public async Task<IActionResult> GetDowntimeSummary([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetDowntimeSummaryAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("downtime-machine")]
        public async Task<IActionResult> GetDowntimeMachine([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetDowntimeMachineAsync(filter,cancellationToken);
            return Ok(result);
        }
        [HttpPost("downtime-reason")]
        public async Task<IActionResult> GetDowntimeReason([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetDowntimeReasonAsync(filter,cancellationToken);
            return Ok(result);
        }
        [HttpPost("downtime-trend")]
        public async Task<IActionResult> GetDowntimeTrend([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetDowntimeTrendAsync(filter,cancellationToken);
            return Ok(result);
        }
        #endregion

        #region Uretim
        [HttpPost("production-summary")]
        public async Task<IActionResult> GetProductionSummary([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetProductionSummaryAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("production-trend")]
        public async Task<IActionResult> GetProductionTrend([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetProductionTrendAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("production-machine")]
        public async Task<IActionResult> GetProductionMachine([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetProductionMachineAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("quality-summary")]
        public async Task<IActionResult> GetQualitySummary([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetQualitySummaryAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("quality-trend")]
        public async Task<IActionResult> GetQualityTrend([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetQualityTrendAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("quality-machine")]
        public async Task<IActionResult> GetQualityMachine([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetQualityMachineAsync(filter,cancellationToken);
            return Ok(result);
        }

        [HttpPost("quality-reason")]
        public async Task<IActionResult> GetQualityReason([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetQualityReasonAsync(filter,cancellationToken);
            return Ok(result);
        }

        #endregion

        #region Dashboard
        [HttpPost("dashboard")]
        public async Task<IActionResult> Dashboard([FromBody] UretimDashboardFilter filter,CancellationToken cancellationToken)
        {
            var result = await _service.GetDashboardAsync(filter, cancellationToken);
            return Ok(result);
        }
        #endregion
    }
}