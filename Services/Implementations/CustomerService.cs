using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetCustomersAsync(
            string? search = null,
            string? status = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(c => c.AssignedToUserId == userIdScope || c.CreatedBy == userIdScope);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         c.Email.ToLower().Contains(term) ||
                                         c.Phone.Contains(term) ||
                                         c.CompanyName.ToLower().Contains(term) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var query = _context.Customers.AsNoTracking().Where(c => c.Email.ToLower() == email.Trim().ToLower());
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var cleanPhone = phone.Trim();
            var query = _context.Customers.AsNoTracking().Where(c => c.Phone == cleanPhone);
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            // Server-side validation
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.", null);
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return (false, "Email is required.", null);
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                return (false, "Phone is required.", null);
            }

            if (!await IsEmailUniqueAsync(customer.Email))
            {
                return (false, "A customer with this email address already exists.", null);
            }

            if (!await IsPhoneUniqueAsync(customer.Phone))
            {
                return (false, "A customer with this phone number already exists.", null);
            }

            // Auto-generate code if empty
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                int maxId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000;
                customer.CustomerCode = $"CUST-{maxId + 1}";
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = currentUserId;

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Customer Creation",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                oldValue: null,
                newValue: $"Name: {customer.CustomerName}, Email: {customer.Email}, Company: {customer.CompanyName}",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Created customer {customer.CustomerCode} ({customer.CustomerName})"
            );

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
            {
                return (false, "Customer not found.");
            }

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return (false, "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                return (false, "Phone is required.");
            }

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
            {
                return (false, "A customer with this email address already exists.");
            }

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
            {
                return (false, "A customer with this phone number already exists.");
            }

            string oldValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            existing.AssignedToUserId = customer.AssignedToUserId;
            existing.Notes = customer.Notes;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = currentUserId;

            await _context.SaveChangesAsync();

            string newValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Customer Update",
                entityName: "Customer",
                recordId: existing.CustomerId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: $"Updated customer {existing.CustomerCode} ({existing.CustomerName})"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                return (false, "Customer not found.");
            }

            string oldValue = $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Status: {customer.Status}";

            // Deactivate customer or remove
            customer.Status = "Inactive";
            customer.ModifiedDate = DateTime.UtcNow;
            customer.ModifiedBy = currentUserId;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Customer Deactivation",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                oldValue: oldValue,
                newValue: "Status: Inactive",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Deactivated customer {customer.CustomerCode} ({customer.CustomerName})"
            );

            return (true, null);
        }
    }
}
