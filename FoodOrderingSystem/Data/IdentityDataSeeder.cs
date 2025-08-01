using FoodOrderingSystem.Models; // We need to use our custom ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

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
                    Console.WriteLine($"Role '{roleName}' created successfully.");
                }
            }

            // --- Seed Admin User ---
            var adminEmail = "admin@mackdihh.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Create new admin user
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var result = await userManager.CreateAsync(adminUser, "Password123!");
                if (result.Succeeded)
                {
                    Console.WriteLine("Admin user created successfully.");
                    
                    // Add to Admin role
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine("Admin user added to Admin role successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add admin user to role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists. Checking password...");
                
                // Check if the password is correct by trying to sign in
                var signInManager = serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                var signInResult = await signInManager.PasswordSignInAsync(adminUser.UserName, "Password123!", false, lockoutOnFailure: false);
                
                if (!signInResult.Succeeded)
                {
                    Console.WriteLine("Admin password is incorrect. Resetting password...");
                    
                    // Reset the password
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, "Password123!");
                    
                    if (resetResult.Succeeded)
                    {
                        Console.WriteLine("Admin password reset successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to reset admin password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine("Admin password is correct.");
                    // Sign out after testing
                    await signInManager.SignOutAsync();
                }
                
                // Ensure admin user is in Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine("Admin user added to Admin role successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add admin user to role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}
