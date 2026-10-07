using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        /// <summary>
        /// Authenticate user via API
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginApiRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync(null, model.Email, "API Login Failure", "Auth", result: "Failed", details: "Invalid credentials or inactive user");
                return Unauthorized(new LoginApiResponseDto { Success = false, Message = "Invalid email or password." });
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _auditService.LogAsync(user.Id, user.Email, "API Login Success", "Auth", result: "Success", details: "User authenticated via REST API");

                return Ok(new LoginApiResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Roles = roles
                });
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.Email, "API Account Lockout", "Auth", result: "Failed", details: "Account locked out");
                return StatusCode(403, new LoginApiResponseDto { Success = false, Message = "Account is locked out due to multiple failed login attempts." });
            }

            await _auditService.LogAsync(user.Id, user.Email, "API Login Failure", "Auth", result: "Failed", details: "Invalid password");
            return Unauthorized(new LoginApiResponseDto { Success = false, Message = "Invalid email or password." });
        }

        /// <summary>
        /// Logout active session
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string? userId = _userManager.GetUserId(User);
            string? userName = User.Identity?.Name;

            await _signInManager.SignOutAsync();
            await _auditService.LogAsync(userId, userName, "API Logout", "Auth", result: "Success", details: "User logged out via REST API");

            return Ok(new { success = true, message = "Logged out successfully." });
        }
    }
}
