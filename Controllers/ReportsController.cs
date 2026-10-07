using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CustomerReport(string? status)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetCustomerReportAsync(status, scope);
            ViewBag.Status = status;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> LeadReport(string? status, string? source)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetLeadReportAsync(status, source, scope);
            ViewBag.Status = status;
            ViewBag.Source = source;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityReport(string? stage)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetOpportunityReportAsync(stage, scope);
            ViewBag.Stage = stage;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> FollowUpReport(string? status, string? type)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetFollowUpReportAsync(status, type, scope);
            ViewBag.Status = status;
            ViewBag.Type = type;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> PipelineReport()
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetPipelineReportAsync(scope);
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> ConversionReport()
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var data = await _reportService.GetConversionReportAsync(scope);
            return View(data);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserActivityReport()
        {
            var data = await _reportService.GetUserActivityReportAsync();
            return View(data);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditReport(string? entityName, string? action)
        {
            var data = await _reportService.GetAuditReportAsync(entityName, action);
            ViewBag.EntityName = entityName;
            ViewBag.Action = action;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(string reportType)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;

            switch (reportType.ToLower())
            {
                case "customer":
                    var custs = await _reportService.GetCustomerReportAsync(null, scope);
                    var custCsv = _reportService.GenerateCsv(
                        custs,
                        new[] { "Customer Code", "Customer Name", "Email", "Phone", "Company", "City", "State", "Status", "Assigned To" },
                        c => new[] { c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.City, c.State, c.Status, c.AssignedTo?.Name ?? "Unassigned" }
                    );
                    return File(custCsv, "text/csv", $"Customer_Report_{DateTime.Now:yyyyMMdd}.csv");

                case "lead":
                    var leads = await _reportService.GetLeadReportAsync(null, null, scope);
                    var leadCsv = _reportService.GenerateCsv(
                        leads,
                        new[] { "Lead Code", "Lead Name", "Email", "Phone", "Company", "Source", "Status", "Priority", "Expected Value", "Assigned To" },
                        l => new[] { l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName, l.Source, l.Status, l.Priority, l.ExpectedValue.ToString("F2"), l.AssignedTo?.Name ?? "Unassigned" }
                    );
                    return File(leadCsv, "text/csv", $"Lead_Report_{DateTime.Now:yyyyMMdd}.csv");

                case "opportunity":
                    var opps = await _reportService.GetOpportunityReportAsync(null, scope);
                    var oppCsv = _reportService.GenerateCsv(
                        opps,
                        new[] { "Opportunity Name", "Customer", "Amount", "Stage", "Probability (%)", "Weighted Pipeline", "Expected Close Date", "Status" },
                        o => new[] { o.OpportunityName, o.Customer?.CustomerName ?? "", o.Amount.ToString("F2"), o.Stage, o.Probability.ToString(), o.WeightedPipeline.ToString("F2"), o.ExpectedCloseDate.ToString("yyyy-MM-dd"), o.Status }
                    );
                    return File(oppCsv, "text/csv", $"Opportunity_Report_{DateTime.Now:yyyyMMdd}.csv");

                case "pipeline":
                    var pipeline = await _reportService.GetPipelineReportAsync(scope);
                    var pipeCsv = _reportService.GenerateCsv(
                        pipeline,
                        new[] { "Stage", "Opportunity Count", "Total Amount", "Weighted Amount" },
                        p => new[] { p.Stage, p.OpportunityCount.ToString(), p.TotalAmount.ToString("F2"), p.WeightedAmount.ToString("F2") }
                    );
                    return File(pipeCsv, "text/csv", $"Pipeline_Report_{DateTime.Now:yyyyMMdd}.csv");

                default:
                    return BadRequest("Invalid report type.");
            }
        }
    }
}
