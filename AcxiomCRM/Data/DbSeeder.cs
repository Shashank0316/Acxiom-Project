using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Manager", "Sales Executive" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.com",
                    Email = "admin@acxiomcrm.com",
                    Name = "System Administrator",
                    EmailConfirmed = true
                };
                var createPowerUser = await userManager.CreateAsync(admin, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
            
            // Seed Manager User
            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            if (managerUser == null)
            {
                var manager = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.com",
                    Email = "manager@acxiomcrm.com",
                    Name = "General Manager",
                    EmailConfirmed = true
                };
                var createManager = await userManager.CreateAsync(manager, "Manager@123");
                if (createManager.Succeeded)
                {
                    await userManager.AddToRoleAsync(manager, "Manager");
                }
            }

            // Seed Sales Exec
            var salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
            if (salesUser == null)
            {
                var sales = new ApplicationUser
                {
                    UserName = "sales@acxiomcrm.com",
                    Email = "sales@acxiomcrm.com",
                    Name = "Sales Executive 1",
                    EmailConfirmed = true
                };
                var createSales = await userManager.CreateAsync(sales, "Sales@123");
                if (createSales.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales, "Sales Executive");
                }
            }
        }
    }
}
