using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public string? Search { get; set; }
        public string? Stage { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class OpportunityCreateEditViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        public string Status { get; set; } = "Open";

        public string? AssignedToUserId { get; set; }
        public string? Source { get; set; }
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CustomersSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersSelectList { get; set; } = new List<SelectListItem>();
    }
}
