using System;
using System.Collections.Generic;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        public string DateFilter { get; set; } = "This Month";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }

        // Chart Data Dictionary / Series
        public Dictionary<string, int> LeadStatusData { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PipelineStageData { get; set; } = new Dictionary<string, int>();
        public List<MonthlySalesItem> MonthlySalesData { get; set; } = new List<MonthlySalesItem>();

        // Lists for recent items on Dashboard
        public IEnumerable<Models.FollowUp> UpcomingFollowUps { get; set; } = new List<Models.FollowUp>();
        public IEnumerable<Models.Opportunity> RecentOpportunities { get; set; } = new List<Models.Opportunity>();
        public IEnumerable<Models.Activity> RecentActivities { get; set; } = new List<Models.Activity>();
    }

    public class MonthlySalesItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalWonAmount { get; set; }
        public int WonCount { get; set; }
    }
}
