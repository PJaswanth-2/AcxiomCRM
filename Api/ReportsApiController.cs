using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsApiController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Get Pipeline Report Data
        /// </summary>
        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport()
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var report = await _reportService.GetPipelineReportAsync(scope);
            return Ok(report);
        }
    }
}
