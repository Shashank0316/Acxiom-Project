using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FollowUpsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private IQueryable<FollowUp> GetAuthorizedQuery()
        {
            var query = _context.FollowUps.AsQueryable();
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                return query.Where(f => f.AssignedToId == userId);
            }
            return query.Where(f => false);
        }

        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            var query = GetAuthorizedQuery();

            int pageSize = 10;
            var totalRecords = await query.CountAsync();
            var items = await query
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .OrderBy(f => f.FollowUpDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;

            return View(items);
        }

        public IActionResult Create(int? customerId, int? leadId)
        {
            ViewBag.CustomerId = customerId;
            ViewBag.LeadId = leadId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,LeadId,FollowUpDate,FollowUpType,Remarks,Status,AssignedToId")] FollowUp followUp)
        {
            if (followUp.FollowUpDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-Up Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                if (User.IsInRole("Sales Executive") && string.IsNullOrEmpty(followUp.AssignedToId))
                {
                    followUp.AssignedToId = GetCurrentUserId();
                }

                _context.Add(followUp);
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Create", EntityName = "FollowUp", CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), NewValue = $"Scheduled {followUp.FollowUpType} for {followUp.FollowUpDate}"
                });

                await _context.SaveChangesAsync();

                if (followUp.CustomerId.HasValue)
                    return RedirectToAction("Details", "Customers", new { id = followUp.CustomerId });
                if (followUp.LeadId.HasValue)
                    return RedirectToAction("Details", "Leads", new { id = followUp.LeadId });

                return RedirectToAction(nameof(Index));
            }
            return View(followUp);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var followUp = await GetAuthorizedQuery().FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return Forbid();
            return View(followUp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FollowUpId,CustomerId,LeadId,FollowUpDate,FollowUpType,Remarks,Status,AssignedToId")] FollowUp followUp)
        {
            if (id != followUp.FollowUpId) return NotFound();

            var existing = await GetAuthorizedQuery().AsNoTracking().FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null) return Forbid();

            if (ModelState.IsValid)
            {
                _context.Update(followUp);
                
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Update", EntityName = "FollowUp", RecordId = id.ToString(), CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), OldValue = existing.Status, NewValue = followUp.Status
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(followUp);
        }
    }
}
