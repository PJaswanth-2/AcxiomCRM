using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }

        [ForeignKey(nameof(OpportunityId))]
        public virtual Opportunity? Opportunity { get; set; }

        [StringLength(450)]
        public string? AssignedToUserId { get; set; }

        [ForeignKey(nameof(AssignedToUserId))]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed"; // Planned, Completed, Cancelled
    }
}
