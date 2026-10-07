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
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            IActivityService activityService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? type, string? status, int page = 1)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _activityService.GetActivitiesAsync(search, type, status, null, null, null, scope, page, 10);

            var vm = new ActivityListViewModel
            {
                Activities = items,
                Search = search,
                Type = type,
                Status = status,
                Page = page,
                PageSize = 10,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            var vm = new ActivityCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = System.DateTime.UtcNow,
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                OpportunitiesSelectList = await GetOpportunitiesSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Status = model.Status
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _activityService.CreateActivityAsync(activity, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to log activity.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var act = await _activityService.GetActivityByIdAsync(id);
            if (act == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && act.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            var vm = new ActivityCreateEditViewModel
            {
                ActivityId = act.ActivityId,
                ActivityType = act.ActivityType,
                Subject = act.Subject,
                Description = act.Description,
                ActivityDate = act.ActivityDate,
                CustomerId = act.CustomerId,
                LeadId = act.LeadId,
                OpportunityId = act.OpportunityId,
                AssignedToUserId = act.AssignedToUserId,
                Status = act.Status,
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                OpportunitiesSelectList = await GetOpportunitiesSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ActivityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var activity = new Activity
            {
                ActivityId = model.ActivityId,
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                AssignedToUserId = model.AssignedToUserId,
                Status = model.Status
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _activityService.UpdateActivityAsync(activity, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update activity.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' updated successfully.";
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

        private async Task<IEnumerable<SelectListItem>> GetOpportunitiesSelectListAsync()
        {
            var items = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
            return items.Select(o => new SelectListItem { Value = o.OpportunityId.ToString(), Text = o.OpportunityName });
        }

        private async Task<IEnumerable<SelectListItem>> GetUsersSelectListAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
            return users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.Name} ({u.Email})" });
        }
    }
}
