using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _opportunityService.GetOpportunitiesAsync(search, stage, status, null, scope, page, 10);

            var vm = new OpportunityListViewModel
            {
                Opportunities = items,
                Search = search,
                Stage = stage,
                Status = status,
                Page = page,
                PageSize = 10,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opportunity == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && opportunity.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            return View(opportunity);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new OpportunityCreateEditViewModel
            {
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            // Extra explicit business validations
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Source = model.Source,
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _opportunityService.CreateOpportunityAsync(opp, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create opportunity.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var opp = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opp == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && opp.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            var vm = new OpportunityCreateEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedToUserId = opp.AssignedToUserId,
                Source = opp.Source,
                Notes = opp.Notes,
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityCreateEditViewModel model)
        {
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId,
                Source = model.Source,
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _opportunityService.UpdateOpportunityAsync(opp, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update opportunity.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _opportunityService.DeleteOpportunityAsync(id, currentUserId, currentUserName, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to cancel opportunity.";
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity cancelled.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetCustomersSelectListAsync()
        {
            var items = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            return items.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" });
        }

        private async Task<IEnumerable<SelectListItem>> GetLeadsSelectListAsync()
        {
            var items = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            return items.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" });
        }

        private async Task<IEnumerable<SelectListItem>> GetUsersSelectListAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
            return users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.Name} ({u.Email})" });
        }
    }
}
