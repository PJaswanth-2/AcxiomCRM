using System.Threading.Tasks;
using AcxiomCRM.Models;
using System.Collections.Generic;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(
            string? userId,
            string? userName,
            string action,
            string entityName,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null,
            string result = "Success",
            string? details = null);

        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? userId = null, string? module = null, string? action = null);
    }
}
