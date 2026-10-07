using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<(IEnumerable<Activity> Items, int TotalCount)> GetActivitiesAsync(
            string? search = null,
            string? type = null,
            string? status = null,
            int? customerId = null,
            int? leadId = null,
            int? opportunityId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10);

        Task<Activity?> GetActivityByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserName, string? ipAddress = null);
    }
}
