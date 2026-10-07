using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(ILeadService leadService, UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, string? priority, int page = 1)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _leadService.GetLeadsAsync(search, status, priority, scope, page, 10);

            var vm = new LeadListViewModel
            {
                Leads = items,
                Search = search,
                Status = status,
                Priority = priority,
                Page = page,
                PageSize = 10,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && lead.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new LeadCreateEditViewModel
            {
                UsersSelectList = await GetUsersSelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _leadService.CreateLeadAsync(lead, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create lead.");
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && lead.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            var vm = new LeadCreateEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedToUserId = lead.AssignedToUserId,
                Notes = lead.Notes,
                UsersSelectList = await GetUsersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadCode = model.LeadCode ?? string.Empty,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedToUserId = model.AssignedToUserId,
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _leadService.UpdateLeadAsync(lead, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update lead.");
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _leadService.DeleteLeadAsync(id, currentUserId, currentUserName, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to delete lead.";
            }
            else
            {
                TempData["SuccessMessage"] = "Lead marked as Lost.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Lead is already converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertConfirmed(int id)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, custId, oppId) = await _leadService.ConvertLeadAsync(id, currentUserId, currentUserName, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Lead conversion failed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "Lead successfully converted to Customer and Opportunity!";
            return RedirectToAction("Details", "Customers", new { id = custId });
        }

        private async Task<IEnumerable<SelectListItem>> GetUsersSelectListAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
            return users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.Name} ({u.Email})" });
        }
    }
}
