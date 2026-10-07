using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // Apply Resource Authorization Query
        private IQueryable<Customer> GetAuthorizedCustomersQuery()
        {
            var query = _context.Customers.AsQueryable();
            
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return query;
            }
            if (User.IsInRole("Sales Executive"))
            {
                var userId = GetCurrentUserId();
                return query.Where(c => c.AssignedToId == userId);
            }
            return query.Where(c => false);
        }

        public async Task<IActionResult> Index(string searchString, string statusFilter, int pageNumber = 1)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatus"] = statusFilter;

            var customers = GetAuthorizedCustomersQuery();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c => c.CustomerName.Contains(searchString) || c.Email.Contains(searchString) || c.Phone.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                customers = customers.Where(c => c.Status == statusFilter);
            }

            int pageSize = 10;
            var totalRecords = await customers.CountAsync();
            var items = await customers
                .Include(c => c.AssignedTo)
                .OrderByDescending(c => c.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;

            return View(items);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,Email,Phone,CompanyName,Address,City,State,AssignedToId")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Business validation: unique email
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
                {
                    ModelState.AddModelError("Email", "Email must be unique.");
                    return View(customer);
                }

                customer.CreatedDate = DateTime.UtcNow;
                customer.Status = "Active";
                customer.CreatedById = GetCurrentUserId();
                
                // If a Sales Exec creates it and doesn't assign, default to self
                if (User.IsInRole("Sales Executive") && string.IsNullOrEmpty(customer.AssignedToId))
                {
                    customer.AssignedToId = customer.CreatedById;
                }

                _context.Add(customer);
                
                // Audit log
                _context.AuditLogs.Add(new AuditLog {
                    Action = "Create", 
                    EntityName = "Customer", 
                    CreatedDate = DateTime.UtcNow,
                    UserId = customer.CreatedById, 
                    NewValue = $"Name: {customer.CustomerName}, Email: {customer.Email}"
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return Forbid(); // Or NotFound if we don't want to expose existence

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,CustomerName,Email,Phone,CompanyName,Address,City,State,Status,AssignedToId")] Customer customer)
        {
            if (id != customer.CustomerId) return NotFound();

            // Enforce resource authorization before editing
            var existingCustomer = await GetAuthorizedCustomersQuery().AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (existingCustomer == null) return Forbid();

            // Additional check: Sales Executive cannot reassign ownership to someone else
            if (User.IsInRole("Sales Executive") && customer.AssignedToId != existingCustomer.AssignedToId)
            {
                ModelState.AddModelError("AssignedToId", "You are not authorized to reassign customers.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Business validation: unique email (excluding self)
                    if (await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.CustomerId != id))
                    {
                        ModelState.AddModelError("Email", "Email must be unique.");
                        return View(customer);
                    }

                    // Preserve original created data
                    customer.CreatedDate = existingCustomer.CreatedDate;
                    customer.CreatedById = existingCustomer.CreatedById;

                    _context.Update(customer);
                    
                    _context.AuditLogs.Add(new AuditLog {
                        Action = "Update", 
                        EntityName = "Customer", 
                        RecordId = id.ToString(), 
                        CreatedDate = DateTime.UtcNow,
                        UserId = GetCurrentUserId(),
                        OldValue = $"Status: {existingCustomer.Status}, Email: {existingCustomer.Email}",
                        NewValue = $"Status: {customer.Status}, Email: {customer.Email}"
                    });

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Deactivate/Delete confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return Forbid();

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return Forbid();

            // Instead of hard delete, we deactivate per CRM best practices
            customer.Status = "Inactive";
            _context.Update(customer);

            _context.AuditLogs.Add(new AuditLog {
                Action = "Deactivate", 
                EntityName = "Customer", 
                RecordId = id.ToString(), 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId()
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await GetAuthorizedCustomersQuery()
                .Include(c => c.AssignedTo)
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return Forbid();

            return View(customer);
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}
