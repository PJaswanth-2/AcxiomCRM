using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PipelineReportDto>> GetPipelineReportAsync(string? userIdScope = null)
        {
            var query = _context.Opportunities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope))
            {
                query = query.Where(o => o.AssignedToUserId == userIdScope);
            }

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var list = new List<PipelineReportDto>();

            foreach (var stage in stages)
            {
                var oppsInStage = query.Where(o => o.Stage == stage);
                int count = await oppsInStage.CountAsync();
                decimal totalAmount = await oppsInStage.SumAsync(o => (decimal?)o.Amount) ?? 0m;
                var rawOpps = await oppsInStage.Select(o => new { o.Amount, o.Probability }).ToListAsync();
                decimal weightedAmount = rawOpps.Sum(o => o.Amount * o.Probability / 100m);

                list.Add(new PipelineReportDto
                {
                    Stage = stage,
                    OpportunityCount = count,
                    TotalAmount = totalAmount,
                    WeightedAmount = weightedAmount
                });
            }

            return list;
        }

        public async Task<IEnumerable<Customer>> GetCustomerReportAsync(string? status = null, string? userIdScope = null)
        {
            var query = _context.Customers.Include(c => c.AssignedTo).AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope)) query = query.Where(c => c.AssignedToUserId == userIdScope || c.CreatedBy == userIdScope);
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<Lead>> GetLeadReportAsync(string? status = null, string? source = null, string? userIdScope = null)
        {
            var query = _context.Leads.Include(l => l.AssignedTo).AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope)) query = query.Where(l => l.AssignedToUserId == userIdScope);
            if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
            if (!string.IsNullOrEmpty(source)) query = query.Where(l => l.Source == source);

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<Opportunity>> GetOpportunityReportAsync(string? stage = null, string? userIdScope = null)
        {
            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedTo).AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope)) query = query.Where(o => o.AssignedToUserId == userIdScope);
            if (!string.IsNullOrEmpty(stage)) query = query.Where(o => o.Stage == stage);

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<FollowUp>> GetFollowUpReportAsync(string? status = null, string? type = null, string? userIdScope = null)
        {
            var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(userIdScope)) query = query.Where(f => f.AssignedUserId == userIdScope);
            if (!string.IsNullOrEmpty(status)) query = query.Where(f => f.Status == status);
            if (!string.IsNullOrEmpty(type)) query = query.Where(f => f.FollowUpType == type);

            return await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<object> GetConversionReportAsync(string? userIdScope = null)
        {
            var query = _context.Leads.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(userIdScope)) query = query.Where(l => l.AssignedToUserId == userIdScope);

            int totalLeads = await query.CountAsync();
            int converted = await query.CountAsync(l => l.Status == "Converted");
            int qualified = await query.CountAsync(l => l.Status == "Qualified");
            int lost = await query.CountAsync(l => l.Status == "Lost");

            double conversionRate = totalLeads > 0 ? Math.Round((double)converted / totalLeads * 100, 2) : 0;

            return new
            {
                TotalLeads = totalLeads,
                ConvertedLeads = converted,
                QualifiedLeads = qualified,
                LostLeads = lost,
                ConversionRatePercent = conversionRate
            };
        }

        public async Task<object> GetUserActivityReportAsync()
        {
            var userActivities = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    Email = u.Email,
                    CustomersCount = u.AssignedCustomers.Count(),
                    LeadsCount = u.AssignedLeads.Count(),
                    OpportunitiesCount = u.AssignedOpportunities.Count(),
                    FollowUpsCount = u.AssignedFollowUps.Count(),
                    ActivitiesCount = u.AssignedActivities.Count()
                })
                .ToListAsync();

            return userActivities;
        }

        public async Task<IEnumerable<AuditLog>> GetAuditReportAsync(string? entityName = null, string? action = null)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(entityName)) query = query.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));

            return await query.OrderByDescending(a => a.CreatedDate).Take(1000).ToListAsync();
        }

        public byte[] GenerateCsv<T>(IEnumerable<T> data, string[] headers, Func<T, string[]> rowSelector)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            foreach (var item in data)
            {
                var row = rowSelector(item);
                var escapedRow = row.Select(field => $"\"{field?.Replace("\"", "\"\"")}\"");
                sb.AppendLine(string.Join(",", escapedRow));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
