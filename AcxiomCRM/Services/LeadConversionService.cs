using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public interface ILeadConversionService
    {
        Task<(bool Success, string ErrorMessage, int? CustomerId)> ConvertLeadAsync(int leadId, string userId, string ipAddress);
    }

    public class LeadConversionService : ILeadConversionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeadConversionService> _logger;

        public LeadConversionService(ApplicationDbContext context, ILogger<LeadConversionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage, int? CustomerId)> ConvertLeadAsync(int leadId, string userId, string ipAddress)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
                
                // Concurrency and existence check
                if (lead == null) return (false, "Lead not found.", null);
                if (lead.Status == "Converted") return (false, "Lead has already been converted.", null);
                if (lead.Status != "Qualified") return (false, "Only Qualified leads can be converted.", null);

                // Validation Rules (No Fake Data)
                if (string.IsNullOrWhiteSpace(lead.Email)) return (false, "Lead must have a valid Email to be converted.", null);
                if (string.IsNullOrWhiteSpace(lead.Phone)) return (false, "Lead must have a valid Phone number to be converted.", null);
                if (lead.ExpectedValue <= 0) return (false, "Lead must have an Expected Value greater than 0 to generate an Opportunity.", null);

                // Duplicate Customer Check
                bool customerExists = await _context.Customers.AnyAsync(c => c.Email == lead.Email);
                if (customerExists) return (false, $"A Customer with the email '{lead.Email}' already exists.", null);

                // 1. Update Lead Status
                var oldStatus = lead.Status;
                lead.Status = "Converted";
                _context.Update(lead);

                // 2. Create Customer
                var customer = new Customer
                {
                    CustomerName = lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedById = userId,
                    AssignedToId = lead.AssignedToId
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(); // Need ID for Opportunity

                // 3. Create Opportunity
                var opportunity = new Opportunity
                {
                    OpportunityName = $"{lead.LeadName} - Opportunity",
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = lead.ExpectedValue,
                    Stage = "Qualification",
                    Probability = 10,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedToId = lead.AssignedToId
                };
                _context.Opportunities.Add(opportunity);

                // 4. Audit Logging
                var timestamp = DateTime.UtcNow;
                _context.AuditLogs.AddRange(
                    new AuditLog { Action = "Update", EntityName = "Lead", RecordId = lead.LeadId.ToString(), OldValue = oldStatus, NewValue = "Converted", UserId = userId, CreatedDate = timestamp, IpAddress = ipAddress },
                    new AuditLog { Action = "Create", EntityName = "Customer", RecordId = customer.CustomerId.ToString(), NewValue = $"Created from Lead {lead.LeadId}", UserId = userId, CreatedDate = timestamp, IpAddress = ipAddress },
                    new AuditLog { Action = "Create", EntityName = "Opportunity", RecordId = opportunity.OpportunityId.ToString(), NewValue = $"Generated with amount {opportunity.Amount}", UserId = userId, CreatedDate = timestamp, IpAddress = ipAddress }
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "", customer.CustomerId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict while converting Lead {LeadId}", leadId);
                return (false, "This lead was modified by another user or process. Please refresh and try again.", null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while converting Lead {LeadId}", leadId);
                return (false, "An unexpected error occurred during conversion. Please try again later.", null);
            }
        }
    }
}
