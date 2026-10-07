using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        
        [MaxLength(50)]
        public string? CustomerCode { get; set; }
        
        [Required, MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required, Phone, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
        
        [MaxLength(150)]
        public string? CompanyName { get; set; }
        
        [MaxLength(250)]
        public string? Address { get; set; }
        
        [MaxLength(50)]
        public string? City { get; set; }
        
        [MaxLength(50)]
        public string? State { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Active";
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public string? CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public ApplicationUser? CreatedBy { get; set; }

        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }
    }

    public class Lead
    {
        [Key]
        public int LeadId { get; set; }
        
        [MaxLength(50)]
        public string? LeadCode { get; set; }
        
        [Required, MaxLength(150)]
        public string LeadName { get; set; } = string.Empty;
        
        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }
        
        [Phone, MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(150)]
        public string? CompanyName { get; set; }
        
        [MaxLength(100)]
        public string? Source { get; set; }
        
        [Required, MaxLength(50)]
        [ConcurrencyCheck]
        public string Status { get; set; } = "New";
        
        [Range(0, double.MaxValue)]
        public decimal ExpectedValue { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }
    }

    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }
        
        [Required, MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;
        
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        
        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public Lead? Lead { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }
        
        [Required, MaxLength(50)]
        public string Stage { get; set; } = "Qualification";
        
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; }
        
        [Required]
        public DateTime ExpectedCloseDate { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Open";
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }
    }

    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }
        
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        
        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public Lead? Lead { get; set; }
        
        [Required]
        public DateTime FollowUpDate { get; set; }
        
        [MaxLength(50)]
        public string FollowUpType { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Remarks { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Planned";
        
        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }
    }

    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }
        
        [Required, MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;
        
        [Required, MaxLength(250)]
        public string Subject { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public DateTime ActivityDate { get; set; }
        
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        
        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public Lead? Lead { get; set; }
        
        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Completed";
    }

    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        
        public string? UserId { get; set; }
        [MaxLength(50)]
        public string? Action { get; set; }
        [MaxLength(100)]
        public string? EntityName { get; set; }
        [MaxLength(100)]
        public string? RecordId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}
