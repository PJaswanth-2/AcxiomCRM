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
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _customerService.GetCustomersAsync(search, status, scope, page, 10);

            var vm = new CustomerListViewModel
            {
                Customers = items,
                Search = search,
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
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && customer.AssignedToUserId != UserScopeHelper.GetUserId(User) && customer.CreatedBy != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new CustomerCreateEditViewModel
            {
                UsersSelectList = await GetUsersSelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _customerService.CreateCustomerAsync(customer, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create customer.");
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            if (UserScopeHelper.IsSalesExecutive(User) && customer.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return Forbid();
            }

            var vm = new CustomerCreateEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                AssignedToUserId = customer.AssignedToUserId,
                Notes = customer.Notes,
                UsersSelectList = await GetUsersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerCode = model.CustomerCode ?? string.Empty,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId,
                Notes = model.Notes
            };

            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _customerService.UpdateCustomerAsync(customer, currentUserId, currentUserName, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update customer.");
                model.UsersSelectList = await GetUsersSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            string currentUserId = UserScopeHelper.GetUserId(User)!;
            string currentUserName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _customerService.DeleteCustomerAsync(id, currentUserId, currentUserName, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to delete customer.";
            }
            else
            {
                TempData["SuccessMessage"] = "Customer deactivated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetUsersSelectListAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
            return users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.Name} ({u.Email})" });
        }
    }
}
