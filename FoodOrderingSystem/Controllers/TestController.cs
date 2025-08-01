using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    [AllowAnonymous]
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TestController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@mackdihh.com");
            var adminUserByUsername = await _userManager.FindByNameAsync("admin");
            
            var isInAdminRole = adminUser != null ? await _userManager.IsInRoleAsync(adminUser, "Admin") : false;
            var isInAdminRoleByUsername = adminUserByUsername != null ? await _userManager.IsInRoleAsync(adminUserByUsername, "Admin") : false;
            
            var totalUsers = await _context.Users.CountAsync();
            var totalMenuItems = await _context.MenuItems.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            ViewBag.AdminByEmail = adminUser;
            ViewBag.AdminByUsername = adminUserByUsername;
            ViewBag.IsInAdminRole = isInAdminRole;
            ViewBag.IsInAdminRoleByUsername = isInAdminRoleByUsername;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalMenuItems = totalMenuItems;
            ViewBag.TotalCategories = totalCategories;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestLogin(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(username);
            }

            if (user != null)
            {
                var isValidPassword = await _userManager.CheckPasswordAsync(user, password);
                var isInAdminRole = await _userManager.IsInRoleAsync(user, "Admin");
                
                return Json(new { 
                    Success = true, 
                    UserExists = true, 
                    ValidPassword = isValidPassword, 
                    IsAdmin = isInAdminRole,
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                });
            }

            return Json(new { Success = true, UserExists = false });
        }

        // Debug action to reset admin password (remove in production)
        [AllowAnonymous]
        public async Task<IActionResult> ResetAdminPassword()
        {
            try
            {
                // Check if admin user exists
                var adminUser = await _userManager.FindByEmailAsync("admin@mackdihh.com");
                
                if (adminUser == null)
                {
                    // Create admin user if it doesn't exist
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = "admin@mackdihh.com",
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "User"
                    };
                    
                    var result = await _userManager.CreateAsync(adminUser, "Password123!");
                    if (!result.Succeeded)
                    {
                        return Content($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                    
                    // Add to Admin role
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    // Reset password for existing admin user
                    var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var result = await _userManager.ResetPasswordAsync(adminUser, token, "Password123!");
                    
                    if (!result.Succeeded)
                    {
                        return Content($"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                
                return Content("Admin password has been reset successfully!<br><br>" +
                             "Username: admin<br>" +
                             "Email: admin@mackdihh.com<br>" +
                             "Password: Password123!<br><br>" +
                             "<a href='/Account/Login'>Go to Login</a>");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
} 