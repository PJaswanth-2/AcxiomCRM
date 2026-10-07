using System.Security.Claims;

namespace AcxiomCRM.Authorization
{
    public static class UserScopeHelper
    {
        public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
        public static bool IsManager(ClaimsPrincipal user) => user.IsInRole("Manager");
        public static bool IsSalesExecutive(ClaimsPrincipal user) => user.IsInRole("SalesExecutive");

        public static string? GetUserId(ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        public static string? GetUserName(ClaimsPrincipal user) =>
            user.Identity?.Name;
    }
}
