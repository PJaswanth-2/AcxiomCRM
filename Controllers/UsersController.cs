using System.Collections.Generic;
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
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? roleFilter)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
            }

            var users = await query.OrderBy(u => u.Name).ToListAsync();
            var itemList = new List<UserItemViewModel>();

            foreach (var u in users)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                string mainRole = userRoles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrEmpty(roleFilter) && !string.Equals(mainRole, roleFilter, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool isLockedOut = await _userManager.IsLockedOutAsync(u);

                itemList.Add(new UserItemViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email!,
                    Role = mainRole,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = u.LockoutEnd
                });
            }

            var vm = new UserListViewModel
            {
                Users = itemList,
                Search = search,
                RoleFilter = roleFilter
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.RolesSelectList = await GetRolesSelectListAsync();
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RolesSelectList = await GetRolesSelectListAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedDate = System.DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
                ViewBag.RolesSelectList = await GetRolesSelectListAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "User Creation",
                entityName: "User",
                recordId: user.Id,
                oldValue: null,
                newValue: $"Email: {user.Email}, Name: {user.Name}, Role: {model.Role}",
                ipAddress: ip,
                result: "Success",
                details: $"Admin created user account {user.Email} with role {model.Role}"
            );

            TempData["SuccessMessage"] = $"User '{user.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            string currentRole = roles.FirstOrDefault() ?? "SalesExecutive";

            var vm = new UserEditViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email!,
                Role = currentRole,
                IsActive = user.IsActive,
                RolesSelectList = await GetRolesSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.RolesSelectList = await GetRolesSelectListAsync();
                return View(model);
            }

            string oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "None";

            user.Name = model.Name;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors) ModelState.AddModelError(string.Empty, err.Description);
                model.RolesSelectList = await GetRolesSelectListAsync();
                return View(model);
            }

            // Update role if changed
            if (oldRole != model.Role)
            {
                if (!string.IsNullOrEmpty(oldRole) && oldRole != "None")
                {
                    await _userManager.RemoveFromRoleAsync(user, oldRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // Password reset if supplied
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            }

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "User Update",
                entityName: "User",
                recordId: user.Id,
                oldValue: $"Role: {oldRole}, IsActive: {!user.IsActive}",
                newValue: $"Role: {model.Role}, IsActive: {user.IsActive}",
                ipAddress: ip,
                result: "Success",
                details: $"Admin updated user account {user.Email}"
            );

            TempData["SuccessMessage"] = $"User '{user.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                string currentUserId = UserScopeHelper.GetUserId(User)!;
                string currentUserName = UserScopeHelper.GetUserName(User)!;
                string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                await _auditService.LogAsync(currentUserId, currentUserName, "Account Unlock", "User", user.Id, null, "Lockout cleared", ip, "Success", $"Unlocked account {user.Email}");
                TempData["SuccessMessage"] = $"Account for '{user.Name}' unlocked.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetRolesSelectListAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name });
        }
    }
}
