using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class FollowUpCreateEditViewModel
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [DataType(DataType.Date)]
        public DateTime FollowUpDate { get; set; } = DateTime.Today;

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Planned";

        public string? AssignedUserId { get; set; }
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CustomersSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> OpportunitiesSelectList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersSelectList { get; set; } = new List<SelectListItem>();
    }
}
