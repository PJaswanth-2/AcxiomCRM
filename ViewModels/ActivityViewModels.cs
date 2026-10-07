using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
        public string? Search { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ActivityCreateEditViewModel
    {
        public int ActivityId { get; set; }

        [Required]
        public string ActivityType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        public string? AssignedToUserId { get; set; }

        [Required]
        public string Status { get; set; } = "Completed";

        public IEnumerable<SelectListItem> CustomersSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> OpportunitiesSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersSelectList { get; set; } = new List<SelectListItem>();
    }
}
