using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public int? OpportunityId { get; set; }
        public string? OpportunityName { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today;

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string Status { get; set; } = "Planned";
        public string? AssignedUserId { get; set; }
        public string? Notes { get; set; }
    }
}
