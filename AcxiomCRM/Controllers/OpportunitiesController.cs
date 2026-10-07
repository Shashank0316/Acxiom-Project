using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OpportunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

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

        public async Task<IActionResult> Index(string searchString, string stageFilter, int pageNumber = 1)
        {
            var opps = GetAuthorizedQuery();

            if (!string.IsNullOrEmpty(searchString))
            {
                opps = opps.Where(o => o.OpportunityName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(stageFilter))
            {
                opps = opps.Where(o => o.Stage == stageFilter);
            }

            int pageSize = 10;
            var totalRecords = await opps.CountAsync();
            var items = await opps
                .Include(o => o.AssignedTo)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStage = stageFilter;

            // Pipeline Value KPI: Weighted Amount (Amount * Probability / 100)
            ViewBag.TotalPipelineValue = await opps.Where(o => o.Status == "Open").SumAsync(o => o.Amount * (o.Probability / 100M));

            return View(items);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OpportunityName,CustomerId,Amount,Stage,Probability,ExpectedCloseDate,AssignedToId")] Opportunity opportunity)
        {
            if (opportunity.ExpectedCloseDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                opportunity.CreatedDate = DateTime.UtcNow;
                opportunity.Status = "Open";
                
                if (User.IsInRole("Sales Executive") && string.IsNullOrEmpty(opportunity.AssignedToId))
                {
                    opportunity.AssignedToId = GetCurrentUserId();
                }

                _context.Add(opportunity);
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Create", EntityName = "Opportunity", CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), NewValue = opportunity.OpportunityName
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(opportunity);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var opportunity = await GetAuthorizedQuery().FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null) return Forbid();
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OpportunityId,OpportunityName,CustomerId,LeadId,Amount,Stage,Probability,ExpectedCloseDate,Status,AssignedToId")] Opportunity opportunity)
        {
            if (id != opportunity.OpportunityId) return NotFound();

            var existingOpp = await GetAuthorizedQuery().AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existingOpp == null) return Forbid();

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for active opportunities.");
            }

            if (ModelState.IsValid)
            {
                opportunity.CreatedDate = existingOpp.CreatedDate;
                _context.Update(opportunity);
                
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Update", EntityName = "Opportunity", RecordId = id.ToString(), CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), OldValue = existingOpp.Stage, NewValue = opportunity.Stage
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(opportunity);
        }
    }
}
