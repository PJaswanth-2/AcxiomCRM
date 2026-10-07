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
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
            IFollowUpService followUpService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, string? type, int page = 1)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _followUpService.GetFollowUpsAsync(search, status, type, null, null, null, scope, page, 10);

            var vm = new FollowUpListViewModel
            {
                FollowUps = items,
                Search = search,
                Status = status,
                Type = type,
                Page = page,
                PageSize = 10,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            var vm = new FollowUpCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.Today,
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                OpportunitiesSelectList = await GetOpportunitiesSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
        {
            // Business Validation
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Status = model.Status,
                AssignedUserId = model.AssignedUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _followUpService.CreateFollowUpAsync(followUp, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create follow-up.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var fu = await _followUpService.GetFollowUpByIdAsync(id);
            if (fu == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && fu.AssignedUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            var vm = new FollowUpCreateEditViewModel
            {
                FollowUpId = fu.FollowUpId,
                CustomerId = fu.CustomerId,
                LeadId = fu.LeadId,
                OpportunityId = fu.OpportunityId,
                FollowUpDate = fu.FollowUpDate,
                FollowUpType = fu.FollowUpType,
                Subject = fu.Subject,
                Status = fu.Status,
                AssignedUserId = fu.AssignedUserId,
                Notes = fu.Notes,
                CustomersSelectList = await GetCustomersSelectListAsync(),
                LeadsSelectList = await GetLeadsSelectListAsync(),
                OpportunitiesSelectList = await GetOpportunitiesSelectListAsync(),
                UsersSelectList = await GetUsersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FollowUpCreateEditViewModel model)
        {
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var fu = new FollowUp
            {
                FollowUpId = model.FollowUpId,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Status = model.Status,
                AssignedUserId = model.AssignedUserId,
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _followUpService.UpdateFollowUpAsync(fu, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update follow-up.");
                model.CustomersSelectList = await GetCustomersSelectListAsync();
                model.LeadsSelectList = await GetLeadsSelectListAsync();
                model.OpportunitiesSelectList = await GetOpportunitiesSelectListAsync();
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Follow-up '{fu.Subject}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? notes)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _followUpService.CompleteFollowUpAsync(id, notes, currentUserId, currentUserName, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to complete follow-up.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as Completed!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? notes)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _followUpService.RescheduleFollowUpAsync(id, newDate, notes, currentUserId, currentUserName, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to reschedule follow-up.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Follow-up rescheduled to {newDate:yyyy-MM-dd}.";
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
