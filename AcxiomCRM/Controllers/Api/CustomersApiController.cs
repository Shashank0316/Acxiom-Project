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
    [Route("api/customers")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string GetCurrentIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
        {
            var customers = await GetAuthorizedCustomersQuery().ToListAsync();
            return Ok(customers.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            var customer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
                if (exists) return Forbid(); // Exists but not authorized
                return NotFound();
            }

            return Ok(MapToDto(customer));
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto dto)
        {
            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
            {
                return Conflict(new { message = "Email must be unique." });
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                CreatedDate = DateTime.UtcNow,
                Status = "Active",
                CreatedById = GetCurrentUserId()
            };

            if (User.IsInRole("Sales Executive"))
            {
                customer.AssignedToId = customer.CreatedById;
            }

            _context.Customers.Add(customer);

            _context.AuditLogs.Add(new AuditLog {
                Action = "ApiCreate", 
                EntityName = "Customer", 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId(), 
                NewValue = $"Name: {customer.CustomerName}, Email: {customer.Email}",
                IpAddress = GetCurrentIpAddress()
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, MapToDto(customer));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto dto)
        {
            var existingCustomer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (existingCustomer == null)
            {
                var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
                if (exists) return Forbid();
                return NotFound();
            }

            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email && c.CustomerId != id))
            {
                return Conflict(new { message = "Email must be unique." });
            }

            var oldStatus = existingCustomer.Status;
            var oldEmail = existingCustomer.Email;

            existingCustomer.CustomerName = dto.CustomerName;
            existingCustomer.Email = dto.Email;
            existingCustomer.Phone = dto.Phone;
            existingCustomer.CompanyName = dto.CompanyName;
            existingCustomer.Address = dto.Address;
            existingCustomer.City = dto.City;
            existingCustomer.State = dto.State;
            existingCustomer.Status = dto.Status;

            _context.AuditLogs.Add(new AuditLog {
                Action = "ApiUpdate", 
                EntityName = "Customer", 
                RecordId = id.ToString(), 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId(),
                OldValue = $"Status: {oldStatus}, Email: {oldEmail}",
                NewValue = $"Status: {dto.Status}, Email: {dto.Email}",
                IpAddress = GetCurrentIpAddress()
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await GetAuthorizedCustomersQuery().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
                if (exists) return Forbid();
                return NotFound();
            }

            // Soft delete
            customer.Status = "Inactive";

            _context.AuditLogs.Add(new AuditLog {
                Action = "ApiDeactivate", 
                EntityName = "Customer", 
                RecordId = id.ToString(), 
                CreatedDate = DateTime.UtcNow,
                UserId = GetCurrentUserId(),
                IpAddress = GetCurrentIpAddress()
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static CustomerDto MapToDto(Customer customer)
        {
            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                AssignedToId = customer.AssignedToId
            };
        }
    }
}
