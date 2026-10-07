using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<(IEnumerable<Lead> Items, int TotalCount)> GetLeadsAsync(
            string? search = null,
            string? status = null,
            string? priority = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10);

        Task<Lead?> GetLeadByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(int leadId, string currentUserId, string currentUserName, string? ipAddress = null);
    }
}
