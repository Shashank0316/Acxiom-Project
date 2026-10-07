using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private IQueryable<Activity> GetAuthorizedQuery()
        {
            var query = _context.Activities.AsQueryable();
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                return query.Where(a => a.AssignedToId == userId);
            }
            return query.Where(a => false);
        }

        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            var query = GetAuthorizedQuery();

            int pageSize = 10;
            var totalRecords = await query.CountAsync();
            var items = await query
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .OrderByDescending(a => a.ActivityDate)
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
        public async Task<IActionResult> Create([Bind("ActivityType,Subject,Description,ActivityDate,CustomerId,LeadId,AssignedToId")] Activity activity)
        {
            if (ModelState.IsValid)
            {
                activity.Status = "Completed";
                if (activity.ActivityDate == default)
                {
                    activity.ActivityDate = DateTime.UtcNow;
                }

                if (User.IsInRole("Sales Executive") && string.IsNullOrEmpty(activity.AssignedToId))
                {
                    activity.AssignedToId = GetCurrentUserId();
                }

                _context.Add(activity);
                
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Create", EntityName = "Activity", CreatedDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId(), NewValue = $"Logged {activity.ActivityType}: {activity.Subject}"
                });

                await _context.SaveChangesAsync();

                if (activity.CustomerId.HasValue)
                    return RedirectToAction("Details", "Customers", new { id = activity.CustomerId });
                if (activity.LeadId.HasValue)
                    return RedirectToAction("Details", "Leads", new { id = activity.LeadId });

                return RedirectToAction(nameof(Index));
            }
            return View(activity);
        }
    }
}
