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
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ActivityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(IEnumerable<Activity> Items, int TotalCount)> GetActivitiesAsync(
            string? search = null,
            string? type = null,
            string? status = null,
            int? customerId = null,
            int? leadId = null,
            int? opportunityId = null,
            string? userIdScope = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(a => a.AssignedToUserId == userIdScope);
            }

            if (customerId.HasValue) query = query.Where(a => a.CustomerId == customerId.Value);
            if (leadId.HasValue) query = query.Where(a => a.LeadId == leadId.Value);
            if (opportunityId.HasValue) query = query.Where(a => a.OpportunityId == opportunityId.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                         (a.Description != null && a.Description.ToLower().Contains(term)) ||
                                         (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(type)) query = query.Where(a => a.ActivityType == type);
            if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.ActivityDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Activity?> GetActivityByIdAsync(int id)
        {
            return await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedTo)
                .FirstOrDefaultAsync(a => a.ActivityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
            {
                return (false, "Subject is required.", null);
            }

            await _context.Activities.AddAsync(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Activity Creation",
                entityName: "Activity",
                recordId: activity.ActivityId.ToString(),
                oldValue: null,
                newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}",
                ipAddress: ipAddress,
                result: "Success",
                details: $"Logged activity '{activity.Subject}'"
            );

            return (true, null, activity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
            if (existing == null)
            {
                return (false, "Activity not found.");
            }

            if (string.IsNullOrWhiteSpace(activity.Subject))
            {
                return (false, "Subject is required.");
            }

            string oldValue = $"Subject: {existing.Subject}, Type: {existing.ActivityType}, Status: {existing.Status}";

            existing.Subject = activity.Subject;
            existing.ActivityType = activity.ActivityType;
            existing.Description = activity.Description;
            existing.ActivityDate = activity.ActivityDate;
            existing.CustomerId = activity.CustomerId;
            existing.LeadId = activity.LeadId;
            existing.OpportunityId = activity.OpportunityId;
            existing.AssignedToUserId = activity.AssignedToUserId;
            existing.Status = activity.Status;

            await _context.SaveChangesAsync();

            string newValue = $"Subject: {existing.Subject}, Type: {existing.ActivityType}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                userName: currentUserName,
                action: "Activity Update",
                entityName: "Activity",
                recordId: existing.ActivityId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ipAddress,
                result: "Success",
                details: $"Updated activity '{existing.Subject}'"
            );

            return (true, null);
        }
    }
}
