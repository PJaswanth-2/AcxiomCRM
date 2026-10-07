using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<PipelineReportDto>> GetPipelineReportAsync(string? userIdScope = null);
        Task<IEnumerable<Customer>> GetCustomerReportAsync(string? status = null, string? userIdScope = null);
        Task<IEnumerable<Lead>> GetLeadReportAsync(string? status = null, string? source = null, string? userIdScope = null);
        Task<IEnumerable<Opportunity>> GetOpportunityReportAsync(string? stage = null, string? userIdScope = null);
        Task<IEnumerable<FollowUp>> GetFollowUpReportAsync(string? status = null, string? type = null, string? userIdScope = null);
        Task<object> GetConversionReportAsync(string? userIdScope = null);
        Task<object> GetUserActivityReportAsync();
        Task<IEnumerable<AuditLog>> GetAuditReportAsync(string? entityName = null, string? action = null);
        byte[] GenerateCsv<T>(IEnumerable<T> data, string[] headers, Func<T, string[]> rowSelector);
    }
}
