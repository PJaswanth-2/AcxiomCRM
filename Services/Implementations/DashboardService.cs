using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(
            string? userIdScope = null,
            string dateFilter = "This Month",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var vm = new DashboardViewModel
            {
                DateFilter = dateFilter,
                StartDate = startDate,
                EndDate = endDate
            };

            // Calculate date range filter
            DateTime now = DateTime.UtcNow;
            DateTime filterStart = DateTime.MinValue;
            DateTime filterEnd = DateTime.MaxValue;

            switch (dateFilter)
            {
                case "Today":
                    filterStart = now.Date;
                    filterEnd = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "This Week":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    filterStart = now.Date.AddDays(-1 * diff);
                    filterEnd = filterStart.AddDays(7).AddTicks(-1);
                    break;
                case "This Month":
                    filterStart = new DateTime(now.Year, now.Month, 1);
                    filterEnd = filterStart.AddMonths(1).AddTicks(-1);
                    break;
                case "Custom Range":
                    if (startDate.HasValue) filterStart = startDate.Value.Date;
                    if (endDate.HasValue) filterEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    break;
                default:
                    // All time or default
                    break;
            }

            // Customer Query
            var custQuery = _context.Customers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(userIdScope)) custQuery = custQuery.Where(c => c.AssignedToUserId == userIdScope || c.CreatedBy == userIdScope);
            if (filterStart > DateTime.MinValue) custQuery = custQuery.Where(c => c.CreatedDate >= filterStart && c.CreatedDate <= filterEnd);
            vm.TotalCustomers = await custQuery.CountAsync();

            // Lead Query
            var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(userIdScope)) leadQuery = leadQuery.Where(l => l.AssignedToUserId == userIdScope);
            if (filterStart > DateTime.MinValue) leadQuery = leadQuery.Where(l => l.CreatedDate >= filterStart && l.CreatedDate <= filterEnd);

            vm.TotalLeads = await leadQuery.CountAsync();
            vm.OpenLeads = await leadQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified");

            // Opportunity Query
            var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(userIdScope)) oppQuery = oppQuery.Where(o => o.AssignedToUserId == userIdScope);
            if (filterStart > DateTime.MinValue) oppQuery = oppQuery.Where(o => o.CreatedDate >= filterStart && o.CreatedDate <= filterEnd);

            vm.TotalOpportunities = await oppQuery.CountAsync();
            vm.OpenOpportunities = await oppQuery.CountAsync(o => o.Status == "Open");
            vm.WonOpportunities = await oppQuery.CountAsync(o => o.Stage == "Won" || o.Status == "Won");
            vm.LostOpportunities = await oppQuery.CountAsync(o => o.Stage == "Lost" || o.Status == "Lost");
            vm.TotalPipelineValue = await oppQuery.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0m;

            // Lead Status Chart Data
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
            foreach (var status in leadStatuses)
            {
                var count = await leadQuery.CountAsync(l => l.Status == status);
                vm.LeadStatusData[status] = count;
            }

            // Opportunity Pipeline Chart Data
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            foreach (var stage in stages)
            {
                var count = await oppQuery.CountAsync(o => o.Stage == stage);
                vm.PipelineStageData[stage] = count;
            }

            // Monthly Sales Chart Data (Last 6 Months)
            for (int i = 5; i >= 0; i--)
            {
                var mDate = now.AddMonths(-i);
                var mStart = new DateTime(mDate.Year, mDate.Month, 1);
                var mEnd = mStart.AddMonths(1).AddTicks(-1);

                var mWonOpps = oppQuery.Where(o => (o.Stage == "Won" || o.Status == "Won") && o.CreatedDate >= mStart && o.CreatedDate <= mEnd);
                decimal sum = await mWonOpps.SumAsync(o => (decimal?)o.Amount) ?? 0m;
                int count = await mWonOpps.CountAsync();

                vm.MonthlySalesData.Add(new MonthlySalesItem
                {
                    Month = mDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    TotalWonAmount = sum,
                    WonCount = count
                });
            }

            // Upcoming Follow-ups (Scoped)
            var fuQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().Where(f => f.Status == "Planned");
            if (!string.IsNullOrEmpty(userIdScope)) fuQuery = fuQuery.Where(f => f.AssignedUserId == userIdScope);
            vm.UpcomingFollowUps = await fuQuery.OrderBy(f => f.FollowUpDate).Take(5).ToListAsync();

            // Recent Opportunities
            vm.RecentOpportunities = await oppQuery.Include(o => o.Customer).OrderByDescending(o => o.CreatedDate).Take(5).ToListAsync();

            // Recent Activities
            var actQuery = _context.Activities.Include(a => a.Customer).Include(a => a.Lead).AsNoTracking();
            if (!string.IsNullOrEmpty(userIdScope)) actQuery = actQuery.Where(a => a.AssignedToUserId == userIdScope);
            vm.RecentActivities = await actQuery.OrderByDescending(a => a.ActivityDate).Take(5).ToListAsync();

            return vm;
        }
    }
}
