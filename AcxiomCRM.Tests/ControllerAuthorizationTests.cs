using System.Security.Claims;
using AcxiomCRM.Controllers;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class ControllerAuthorizationTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private void SetUserContext(Controller controller, string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task TestA_SalesExecutive_CanAccessOwnLead()
        {
            var db = GetDbContext("TestA_DB");
            db.Leads.Add(new Lead { LeadId = 1, LeadName = "Lead A", Email = "a@a.com", Phone="123", AssignedToId = "UserA", Status = "New" });
            await db.SaveChangesAsync();

            var controller = new LeadsController(db, null);
            SetUserContext(controller, "UserA", "Sales Executive");

            var result = await controller.Edit(1) as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<Lead>(result.Model);
        }

        [Fact]
        public async Task TestB_SalesExecutive_CannotAccessOtherLead()
        {
            var db = GetDbContext("TestB_DB");
            db.Leads.Add(new Lead { LeadId = 2, LeadName = "Lead B", Email = "b@b.com", Phone="123", AssignedToId = "UserB", Status = "New" });
            await db.SaveChangesAsync();

            var controller = new LeadsController(db, null);
            SetUserContext(controller, "UserA", "Sales Executive");

            var result = await controller.Edit(2);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task TestC_TamperedLeadConversionRequest()
        {
            var db = GetDbContext("TestC_DB");
            db.Leads.Add(new Lead { LeadId = 3, LeadName = "Lead B", Email = "b@b.com", Phone="123", AssignedToId = "UserB", Status = "Qualified" });
            await db.SaveChangesAsync();

            var mockService = new Mock<AcxiomCRM.Services.ILeadConversionService>();
            var controller = new LeadsController(db, mockService.Object);
            SetUserContext(controller, "UserA", "Sales Executive");

            var result = await controller.Convert(3);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task TestF_ManagerAuthorization()
        {
            var db = GetDbContext("TestF_DB");
            db.Leads.Add(new Lead { LeadId = 4, LeadName = "Lead B", Email = "b@b.com", Phone="123", AssignedToId = "UserB", Status = "Qualified" });
            await db.SaveChangesAsync();

            var controller = new LeadsController(db, null);
            SetUserContext(controller, "ManagerA", "Manager"); // Role is Manager

            var result = await controller.Edit(4) as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<Lead>(result.Model);
        }

        [Fact]
        public async Task TestG_AdminAuthorization()
        {
            var db = GetDbContext("TestG_DB");
            db.Leads.Add(new Lead { LeadId = 5, LeadName = "Lead B", Email = "b@b.com", Phone="123", AssignedToId = "UserB", Status = "Qualified" });
            await db.SaveChangesAsync();

            var controller = new LeadsController(db, null);
            SetUserContext(controller, "AdminA", "Admin"); // Role is Admin

            var result = await controller.Edit(5) as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<Lead>(result.Model);
        }

        [Fact]
        public async Task TestD_CustomerOwnership_Authorization()
        {
            var db = GetDbContext("TestD_DB");
            db.Customers.Add(new Customer { CustomerId = 1, CustomerName = "Cust B", Email = "b@b.com", Phone="123", AssignedToId = "UserB", Status = "Active" });
            await db.SaveChangesAsync();

            var controller = new CustomersController(db, null);
            SetUserContext(controller, "UserA", "Sales Executive");

            var result = await controller.Edit(1);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task TestE_OpportunityOwnership_Authorization()
        {
            var db = GetDbContext("TestE_DB");
            db.Opportunities.Add(new Opportunity { OpportunityId = 1, OpportunityName = "Opp B", AssignedToId = "UserB", Status = "Open" });
            await db.SaveChangesAsync();

            var controller = new OpportunitiesController(db);
            SetUserContext(controller, "UserA", "Sales Executive");

            var result = await controller.Edit(1);
            Assert.IsType<ForbidResult>(result);
        }
    }
}
