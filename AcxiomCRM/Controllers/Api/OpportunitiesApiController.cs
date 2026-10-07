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
    [Route("api/opportunities")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OpportunitiesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string GetCurrentIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        private IQueryable<Opportunity> GetAuthorizedQuery()
        {
            var query = _context.Opportunities.AsQueryable();
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                return query.Where(o => o.AssignedToId == userId);
            }
            return query.Where(o => false);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities()
        {
            var items = await GetAuthorizedQuery().ToListAsync();
            return Ok(items.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityDto>> GetOpportunity(int id)
        {
            var item = await GetAuthorizedQuery().FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (item == null)
            {
                var exists = await _context.Opportunities.AnyAsync(o => o.OpportunityId == id);
                if (exists) return Forbid();
                return NotFound();
            }

            return Ok(MapToDto(item));
        }

        [HttpPost]
        public async Task<ActionResult<OpportunityDto>> CreateOpportunity(CreateOpportunityDto dto)
        {
            if (dto.ExpectedCloseDate < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "Expected Close Date cannot be in the past." });
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Stage = "Prospecting", // Default stage or whatever
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow
            };

            if (User.IsInRole("Sales Executive"))
            {
                opp.AssignedToId = GetCurrentUserId();
            }

            _context.Opportunities.Add(opp);

            _context.AuditLogs.Add(new AuditLog {
                Action = "ApiCreate", 
                EntityName = "Opportunity", 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId(), 
                NewValue = opp.OpportunityName,
                IpAddress = GetCurrentIpAddress()
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, MapToDto(opp));
        }

        private static OpportunityDto MapToDto(Opportunity opp)
        {
            return new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedToId = opp.AssignedToId
            };
        }
    }
}
