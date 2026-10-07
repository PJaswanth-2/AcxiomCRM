using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<(IEnumerable<FollowUp> Items, int TotalCount)> GetFollowUpsAsync(
            string? search = null,
            string? status = null,
            string? type = null,
            int? customerId = null,
            int? leadId = null,
            int? opportunityId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10);

        Task<FollowUp?> GetFollowUpByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? notes, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, System.DateTime newDate, string? notes, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<IEnumerable<FollowUp>> GetUpcomingFollowUpsAsync(string? userIdScope = null, int count = 5);
        Task<IEnumerable<FollowUp>> GetOverdueFollowUpsAsync(string? userIdScope = null, int count = 5);
    }
}
