using System;
using System.Threading.Tasks;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(
            string? userIdScope = null,
            string dateFilter = "This Month",
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
