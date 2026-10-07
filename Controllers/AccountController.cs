using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                await _auditService.LogAsync(null, model.Email, "Login Failure", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Failed", details: "User not found");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
                await _auditService.LogAsync(user.Id, user.Email, "Login Failure", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Failed", details: "Inactive account login attempt");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Login Success", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Success", details: "User logged in successfully");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Please try again later.");
                await _auditService.LogAsync(user.Id, user.Email, "Account Lockout", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Failed", details: "Account locked out");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            await _auditService.LogAsync(user.Id, user.Email, "Login Failure", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Failed", details: "Incorrect password");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                IsActive = true,
                CreatedDate = System.DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Role assignment: restrict to allowed roles
                string role = string.Equals(model.Role, "Admin", System.StringComparison.OrdinalIgnoreCase) ? "Admin" :
                             string.Equals(model.Role, "Manager", System.StringComparison.OrdinalIgnoreCase) ? "Manager" : "SalesExecutive";

                await _userManager.AddToRoleAsync(user, role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                await _auditService.LogAsync(user.Id, user.Email, "User Registration", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Success", details: $"Registered new user with role {role}");

                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? userId = _userManager.GetUserId(User);
            string? userName = User.Identity?.Name;

            await _signInManager.SignOutAsync();
            await _auditService.LogAsync(userId, userName, "Logout", "Auth", ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), result: "Success", details: "User logged out");

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
