using FoodOrderingSystem.Models; // We need to use our custom ApplicationUser
using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Data
{
    public static class IdentityDataSeeder
    {
        // This method now uses ApplicationUser
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            // We ask for the UserManager that works with ApplicationUser
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // --- Seed Roles ---
            string[] roleNames = { "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- Seed Admin User ---
            var adminEmail = "admin@mackdihh.com";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                // We create a new ApplicationUser
                var newAdminUser = new ApplicationUser()
                {
                    UserName = "admin", // Set the username
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdminUser, "Password123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
        }
    }
}
