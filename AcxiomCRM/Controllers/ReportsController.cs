using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using System.Security.Claims;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private IQueryable<T> ApplyRoleFilter<T>(IQueryable<T> query) where T : class
        {
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                if (typeof(T) == typeof(Lead))
                    return query.Cast<Lead>().Where(l => l.AssignedToId == userId).Cast<T>();
                if (typeof(T) == typeof(Opportunity))
                    return query.Cast<Opportunity>().Where(o => o.AssignedToId == userId).Cast<T>();
            }
            return query.Where(x => false);
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Pipeline()
        {
            var opps = await ApplyRoleFilter(_context.Opportunities)
                .Include(o => o.AssignedTo)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.ExpectedCloseDate)
                .ToListAsync();

            return View(opps);
        }

        public async Task<IActionResult> LeadConversion()
        {
            var leads = await ApplyRoleFilter(_context.Leads)
                .Include(l => l.AssignedTo)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

            return View(leads);
        }
    }
}
