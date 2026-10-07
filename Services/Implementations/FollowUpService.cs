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
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(IEnumerable<FollowUp> Items, int TotalCount)> GetFollowUpsAsync(
            string? search = null,
            string? status = null,
            string? type = null,
            int? customerId = null,
            int? leadId = null,
            int? opportunityId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(f => f.AssignedUserId == userIdScope);
            }

            if (customerId.HasValue) query = query.Where(f => f.CustomerId == customerId.Value);
            if (leadId.HasValue) query = query.Where(f => f.LeadId == leadId.Value);
            if (opportunityId.HasValue) query = query.Where(f => f.OpportunityId == opportunityId.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)) ||
                                         (f.Opportunity != null && f.Opportunity.OpportunityName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(status)) query = query.Where(f => f.Status == status);
            if (!string.IsNullOrEmpty(type)) query = query.Where(f => f.FollowUpType == type);

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(f => f.FollowUpDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id)
        {
            return await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(followUp.Subject))
            {
                return (false, "Subject is required.", null);
            }

            // Rule: A new/planned follow-up cannot have a date earlier than today
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                return (false, "Follow-up date cannot be earlier than today.", null);
            }

            followUp.CreatedDate = DateTime.UtcNow;

            await _context.FollowUps.AddAsync(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "FollowUp Creation",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: null,
                newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:yyyy-MM-dd}, Type: {followUp.FollowUpType}, Status: {followUp.Status}",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Scheduled follow-up '{followUp.Subject}' for {followUp.FollowUpDate:yyyy-MM-dd}"
            );

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
            if (existing == null)
            {
                return (false, "Follow-up not found.");
            }

            if (string.IsNullOrWhiteSpace(followUp.Subject))
            {
                return (false, "Subject is required.");
            }

            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                return (false, "Follow-up date cannot be earlier than today.");
            }

            string oldValue = $"Subject: {existing.Subject}, Date: {existing.FollowUpDate:yyyy-MM-dd}, Status: {existing.Status}";

            existing.Subject = followUp.Subject;
            existing.FollowUpType = followUp.FollowUpType;
            existing.FollowUpDate = followUp.FollowUpDate;
            existing.Status = followUp.Status;
            existing.AssignedUserId = followUp.AssignedUserId;
            existing.CustomerId = followUp.CustomerId;
            existing.LeadId = followUp.LeadId;
            existing.OpportunityId = followUp.OpportunityId;
            existing.Notes = followUp.Notes;

            await _context.SaveChangesAsync();

            string newValue = $"Subject: {existing.Subject}, Date: {existing.FollowUpDate:yyyy-MM-dd}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "FollowUp Update",
                entityName: "FollowUp",
                recordId: existing.FollowUpId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: $"Updated follow-up '{existing.Subject}'"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? notes, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return (false, "Follow-up not found.");
            }

            string oldValue = $"Status: {followUp.Status}";
            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                followUp.Notes = (followUp.Notes ?? "") + $"\n[Completed on {DateTime.Now:yyyy-MM-dd HH:mm}]: {notes}";
            }

            // Create corresponding Activity record
            var activity = new Activity
            {
                ActivityType = followUp.FollowUpType,
                Subject = $"Completed: {followUp.Subject}",
                Description = notes ?? followUp.Notes ?? "Completed follow-up",
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                AssignedToUserId = followUp.AssignedUserId ?? currentUserId,
                Status = "Completed"
            };
            await _context.Activities.AddAsync(activity);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "FollowUp Completion",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: oldValue,
                newValue: "Status: Completed",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Completed follow-up '{followUp.Subject}'"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? notes, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            if (newDate.Date < DateTime.Today)
            {
                return (false, "Follow-up date cannot be earlier than today.");
            }

            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return (false, "Follow-up not found.");
            }

            string oldValue = $"Date: {followUp.FollowUpDate:yyyy-MM-dd}, Status: {followUp.Status}";

            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                followUp.Notes = (followUp.Notes ?? "") + $"\n[Rescheduled to {newDate:yyyy-MM-dd}]: {notes}";
            }

            await _context.SaveChangesAsync();

            string newValue = $"Date: {newDate:yyyy-MM-dd}, Status: Planned";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "FollowUp Reschedule",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: $"Rescheduled follow-up '{followUp.Subject}' to {newDate:yyyy-MM-dd}"
            );

            return (true, null);
        }

        public async Task<IEnumerable<FollowUp>> GetUpcomingFollowUpsAsync(string? userIdScope = null, int count = 5)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .AsNoTracking()
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= DateTime.Today);

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(f => f.AssignedUserId == userIdScope);
            }

            return await query.OrderBy(f => f.FollowUpDate).Take(count).ToListAsync();
        }

        public async Task<IEnumerable<FollowUp>> GetOverdueFollowUpsAsync(string? userIdScope = null, int count = 5)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .AsNoTracking()
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date < DateTime.Today);

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(f => f.AssignedUserId == userIdScope);
            }

            return await query.OrderBy(f => f.FollowUpDate).Take(count).ToListAsync();
        }
    }
}
