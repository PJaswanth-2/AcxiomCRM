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
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(IEnumerable<Opportunity> Items, int TotalCount)> GetOpportunitiesAsync(
            string? search = null,
            string? stage = null,
            string? status = null,
            int? customerId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(o => o.AssignedToUserId == userIdScope);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                         (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.CreatedDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id)
        {
            return await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .Include(o => o.FollowUps)
                .Include(o => o.Activities)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            // Business Validation
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            {
                return (false, "Opportunity Name is required.", null);
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }

            opportunity.CreatedDate = DateTime.UtcNow;

            await _context.Opportunities.AddAsync(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Opportunity Creation",
                entityName: "Opportunity",
                recordId: opportunity.OpportunityId.ToString(),
                oldValue: null,
                newValue: $"Name: {opportunity.OpportunityName}, Amount: ${opportunity.Amount}, Stage: {opportunity.Stage}, Probability: {opportunity.Probability}%",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Created opportunity {opportunity.OpportunityName}"
            );

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
            {
                return (false, "Opportunity not found.");
            }

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            {
                return (false, "Opportunity Name is required.");
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.");
            }

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.");
            }

            string oldStage = existing.Stage;
            string oldValue = $"Name: {existing.OpportunityName}, Amount: ${existing.Amount}, Stage: {existing.Stage}, Probability: {existing.Probability}%, Status: {existing.Status}";

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Status = opportunity.Status;
            existing.AssignedToUserId = opportunity.AssignedToUserId;
            existing.Source = opportunity.Source;
            existing.Notes = opportunity.Notes;

            // Automatically set status if Stage is Won/Lost
            if (existing.Stage == "Won") existing.Status = "Won";
            else if (existing.Stage == "Lost") existing.Status = "Lost";

            await _context.SaveChangesAsync();

            string newValue = $"Name: {existing.OpportunityName}, Amount: ${existing.Amount}, Stage: {existing.Stage}, Probability: {existing.Probability}%, Status: {existing.Status}";

            string action = oldStage != existing.Stage ? "Opportunity Stage Change" : "Opportunity Update";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: action,
                entityName: "Opportunity",
                recordId: existing.OpportunityId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: oldStage != existing.Stage
                    ? $"Opportunity {existing.OpportunityName} stage changed from {oldStage} to {existing.Stage}"
                    : $"Updated opportunity {existing.OpportunityName}"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
            {
                return (false, "Opportunity not found.");
            }

            string oldValue = $"Name: {opportunity.OpportunityName}, Amount: ${opportunity.Amount}, Status: {opportunity.Status}";

            opportunity.Status = "Cancelled";
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Opportunity Cancellation",
                entityName: "Opportunity",
                recordId: opportunity.OpportunityId.ToString(),
                oldValue: oldValue,
                newValue: "Status: Cancelled",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Cancelled opportunity {opportunity.OpportunityName}"
            );

            return (true, null);
        }
    }
}
