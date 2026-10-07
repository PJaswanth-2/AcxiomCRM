using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<(IEnumerable<Opportunity> Items, int TotalCount)> GetOpportunitiesAsync(
            string? search = null,
            string? stage = null,
            string? status = null,
            int? customerId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10);

        Task<Opportunity?> GetOpportunityByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null);
    }
}
