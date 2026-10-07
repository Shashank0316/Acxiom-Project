using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeadConversionService _conversionService;

        public LeadsController(ApplicationDbContext context, ILeadConversionService conversionService)
        {
            _context = context;
            _conversionService = conversionService;
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
            return query.Where(l => false); // Restrict unknown roles
        }

        public async Task<IActionResult> Index(string searchString, string statusFilter, string companyFilter, string assignedToFilter, int pageNumber = 1)
        {
            if (pageNumber < 1) pageNumber = 1;

            var leads = GetAuthorizedLeadsQuery();

            if (!string.IsNullOrEmpty(searchString))
            {
                leads = leads.Where(l => l.LeadName.Contains(searchString) || l.Email.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(statusFilter))
            {
                leads = leads.Where(l => l.Status == statusFilter);
            }
            if (!string.IsNullOrEmpty(companyFilter))
            {
                leads = leads.Where(l => l.CompanyName.Contains(companyFilter));
            }
            if (!string.IsNullOrEmpty(assignedToFilter))
            {
                leads = leads.Where(l => l.AssignedTo.Name.Contains(assignedToFilter));
            }

            int pageSize = 10;
            var totalRecords = await leads.CountAsync();
            var items = await leads
                .Include(l => l.AssignedTo)
                .OrderByDescending(l => l.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<Lead>
            {
                Items = items,
                TotalCount = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                StatusFilter = statusFilter,
                CompanyFilter = companyFilter,
                AssignedToFilter = assignedToFilter
            };

            return View(pagedResult);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LeadName,Email,Phone,CompanyName,Source,ExpectedValue,AssignedToId")] Lead lead)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Leads.AnyAsync(l => l.Email == lead.Email))
                {
                    ModelState.AddModelError("Email", "A lead with this email already exists.");
                    return View(lead);
                }

                lead.CreatedDate = DateTime.UtcNow;
                lead.Status = "New";
                
                if (User.IsInRole("Sales Executive") && string.IsNullOrEmpty(lead.AssignedToId))
                {
                    lead.AssignedToId = GetCurrentUserId();
                }

                _context.Add(lead);
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Create", EntityName = "Lead", CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), NewValue = lead.LeadName, IpAddress = GetCurrentIpAddress()
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lead);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var leadExists = await _context.Leads.AnyAsync(l => l.LeadId == id);
            if (!leadExists) return NotFound();

            var lead = await GetAuthorizedLeadsQuery().FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return Forbid(); // 403 because it exists but user isn't authorized

            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LeadId,LeadName,Email,Phone,CompanyName,Source,Status,ExpectedValue,AssignedToId")] Lead lead)
        {
            if (id != lead.LeadId) return NotFound();

            var exists = await _context.Leads.AnyAsync(l => l.LeadId == id);
            if (!exists) return NotFound();

            var existingLead = await GetAuthorizedLeadsQuery().AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id);
            if (existingLead == null) return Forbid();

            if (User.IsInRole("Sales Executive") && lead.AssignedToId != existingLead.AssignedToId)
            {
                ModelState.AddModelError("AssignedToId", "You cannot reassign leads.");
            }

            if (ModelState.IsValid)
            {
                lead.CreatedDate = existingLead.CreatedDate;
                _context.Update(lead);
                
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Update", EntityName = "Lead", RecordId = id.ToString(), CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), OldValue = existingLead.Status, NewValue = lead.Status, IpAddress = GetCurrentIpAddress()
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id)
        {
            var exists = await _context.Leads.AnyAsync(l => l.LeadId == id);
            if (!exists) return NotFound();

            var lead = await GetAuthorizedLeadsQuery().FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return Forbid();

            var result = await _conversionService.ConvertLeadAsync(id, GetCurrentUserId(), GetCurrentIpAddress());

            if (result.Success)
            {
                TempData["Success"] = "Lead successfully converted to Customer and Opportunity.";
                return RedirectToAction("Details", "Customers", new { id = result.CustomerId });
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
