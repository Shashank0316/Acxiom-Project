using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/leads")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeadsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string GetCurrentIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        private IQueryable<Lead> GetAuthorizedLeadsQuery()
        {
            var query = _context.Leads.AsQueryable();
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                return query.Where(l => l.AssignedToId == userId);
            }
            return query.Where(l => false);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads()
        {
            var leads = await GetAuthorizedLeadsQuery().ToListAsync();
            return Ok(leads.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDto>> GetLead(int id)
        {
            var lead = await GetAuthorizedLeadsQuery().FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
            {
                var exists = await _context.Leads.AnyAsync(l => l.LeadId == id);
                if (exists) return Forbid();
                return NotFound();
            }

            return Ok(MapToDto(lead));
        }

        [HttpPost]
        public async Task<ActionResult<LeadDto>> CreateLead(CreateLeadDto dto)
        {
            if (dto.Email != null && await _context.Leads.AnyAsync(l => l.Email == dto.Email))
            {
                return Conflict(new { message = "Email already exists for another lead." });
            }

            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                ExpectedValue = dto.ExpectedValue,
                Status = "New",
                CreatedDate = DateTime.UtcNow
            };

            if (User.IsInRole("Sales Executive"))
            {
                lead.AssignedToId = GetCurrentUserId();
            }

            _context.Leads.Add(lead);

            _context.AuditLogs.Add(new AuditLog {
                Action = "ApiCreate", 
                EntityName = "Lead", 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId(), 
                NewValue = lead.LeadName,
                IpAddress = GetCurrentIpAddress()
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, MapToDto(lead));
        }

        private static LeadDto MapToDto(Lead lead)
        {
            return new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedToId = lead.AssignedToId
            };
        }
    }
}
