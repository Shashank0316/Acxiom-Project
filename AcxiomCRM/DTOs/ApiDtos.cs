using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedToId { get; set; }
    }

    public class CreateCustomerDto
    {
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
    }

    public class UpdateCustomerDto : CreateCustomerDto
    {
        [Required, MaxLength(50)]
        public string Status { get; set; } = string.Empty;
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string? LeadCode { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? Source { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedToId { get; set; }
    }

    public class CreateLeadDto
    {
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
        
        [Range(0, double.MaxValue)]
        public decimal ExpectedValue { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedToId { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required, MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;
        
        public int? CustomerId { get; set; }
        
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Range(0, 100)]
        public int Probability { get; set; }
        
        [Required]
        public DateTime ExpectedCloseDate { get; set; }
    }
}
