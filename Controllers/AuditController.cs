using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? userId, string? module, string? action)
        {
            var logs = await _auditService.GetAuditLogsAsync(userId, module, action);
            ViewBag.Module = module;
            ViewBag.Action = action;
            return View(logs);
        }
    }
}
