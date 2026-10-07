using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<(IEnumerable<Customer> Items, int TotalCount)> GetCustomersAsync(
            string? search = null,
            string? status = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10);

        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
    }
}
