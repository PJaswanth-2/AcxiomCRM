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
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomerService _customerService;
        private readonly IAuditService _auditService;

        public LeadService(ApplicationDbContext context, ICustomerService customerService, IAuditService auditService)
        {
            _context = context;
            _customerService = customerService;
            _auditService = auditService;
        }

        public async Task<(IEnumerable<Lead> Items, int TotalCount)> GetLeadsAsync(
            string? search = null,
            string? status = null,
            string? priority = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.ConvertedCustomer)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(l => l.AssignedToUserId == userIdScope);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         l.Email.ToLower().Contains(term) ||
                                         l.Phone.Contains(term) ||
                                         l.CompanyName.ToLower().Contains(term) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(l => l.Priority == priority);
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Lead?> GetLeadByIdAsync(int id)
        {
            return await _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead Name is required.", null);
            }

            if (string.IsNullOrWhiteSpace(lead.Email))
            {
                return (false, "Email is required.", null);
            }

            if (string.IsNullOrWhiteSpace(lead.Phone))
            {
                return (false, "Phone is required.", null);
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected Value must be greater than or equal to 0.", null);
            }

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                int maxId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 2000;
                lead.LeadCode = $"LEAD-{maxId + 1}";
            }

            lead.CreatedDate = DateTime.UtcNow;

            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Lead Creation",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: null,
                newValue: $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}, Value: ${lead.ExpectedValue}",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Created lead {lead.LeadCode} ({lead.LeadName})"
            );

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
            {
                return (false, "Lead not found.");
            }

            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead Name is required.");
            }

            if (string.IsNullOrWhiteSpace(lead.Email))
            {
                return (false, "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(lead.Phone))
            {
                return (false, "Phone is required.");
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected Value must be greater than or equal to 0.");
            }

            string oldValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Priority: {existing.Priority}, Value: ${existing.ExpectedValue}";

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.AssignedToUserId = lead.AssignedToUserId;
            existing.Notes = lead.Notes;

            await _context.SaveChangesAsync();

            string newValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Priority: {existing.Priority}, Value: ${existing.ExpectedValue}";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Lead Update",
                entityName: "Lead",
                recordId: existing.LeadId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: $"Updated lead {existing.LeadCode} ({existing.LeadName})"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return (false, "Lead not found.");
            }

            string oldValue = $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}";

            lead.Status = "Lost";
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Lead Deactivation",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: oldValue,
                newValue: "Status: Lost",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Marked lead {lead.LeadCode} as Lost"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(int leadId, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (lead.Status == "Converted")
            {
                return (false, "Lead is already converted.", lead.ConvertedCustomerId, null);
            }

            // Create new Customer
            int maxCustId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000;
            var customer = new Customer
            {
                CustomerCode = $"CUST-{maxCustId + 1}",
                CustomerName = string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.LeadName : lead.CompanyName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId,
                AssignedToUserId = lead.AssignedToUserId ?? currentUserId,
                Notes = $"Converted from Lead {lead.LeadCode}. Notes: {lead.Notes}"
            };

            var (custSuccess, custErr, createdCust) = await _customerService.CreateCustomerAsync(customer, currentUserId, currentUserName, ipAddress);
            if (!custSuccess || createdCust == null)
            {
                return (false, custErr ?? "Failed to create customer from lead.", null, null);
            }

            // Create Opportunity
            var opportunity = new Opportunity
            {
                OpportunityName = $"{createdCust.CustomerName} - Sales Deal",
                CustomerId = createdCust.CustomerId,
                LeadId = lead.LeadId,
                Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
                Stage = "Qualification",
                Probability = 20,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedToUserId = lead.AssignedToUserId ?? currentUserId,
                Source = lead.Source,
                Notes = $"Auto-created during conversion of Lead {lead.LeadCode}."
            };

            await _context.Opportunities.AddAsync(opportunity);

            // Update Lead
            lead.Status = "Converted";
            lead.ConvertedCustomerId = createdCust.CustomerId;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Lead Conversion",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: "Status: Qualified/New",
                newValue: $"Converted to CustomerId: {createdCust.CustomerId}, OpportunityId: {opportunity.OpportunityId}",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Converted Lead {lead.LeadCode} to Customer {createdCust.CustomerCode} and Opportunity {opportunity.OpportunityName}"
            );

            return (true, null, createdCust.CustomerId, opportunity.OpportunityId);
        }
    }
}
