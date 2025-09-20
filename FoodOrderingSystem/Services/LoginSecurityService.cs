using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services
{
    public class LoginSecurityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginSecurityService> _logger;

        public LoginSecurityService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginSecurityService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> IsUserBlockedAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if login attempts should be reset (after 30 minutes of inactivity)
            if (user.LastLoginAttempt.HasValue && 
                user.LastLoginAttempt.Value.AddMinutes(30) < DateTime.UtcNow && 
                user.LoginAttempts > 0)
            {
                user.LoginAttempts = 0;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Login attempts reset for user {UserId} after 30 minutes of inactivity", userId);
            }

            if (!user.IsBlocked) return false;

            // Check if block has expired
            if (user.BlockedUntil.HasValue && user.BlockedUntil.Value < DateTime.UtcNow)
            {
                user.IsBlocked = false;
                user.BlockedUntil = null;
                user.BlockReason = null;
                user.LoginAttempts = 0;
                await _userManager.UpdateAsync(user);
                return false;
            }

            return true;
        }

        public async Task<bool> IsUserBlockedByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            return await IsUserBlockedAsync(user.Id);
        }

        public async Task<bool> IsUserBlockedByUsernameAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;

            return await IsUserBlockedAsync(user.Id);
        }

        public async Task RecordFailedLoginAttemptAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LoginAttempts++;
            user.LastLoginAttempt = DateTime.UtcNow;

            // Check if user is admin - admins should not be blocked
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            // Block user after 5 failed attempts for 30 minutes (but not admins)
            if (user.LoginAttempts >= 5 && !isAdmin)
            {
                user.IsBlocked = true;
                user.BlockedUntil = DateTime.UtcNow.AddMinutes(30);
                user.BlockReason = "Too many failed login attempts";
                _logger.LogWarning("User {UserId} blocked due to {LoginAttempts} failed login attempts", userId, user.LoginAttempts);
            }
            else if (user.LoginAttempts >= 5 && isAdmin)
            {
                // Log warning for admin but don't block
                _logger.LogWarning("Admin user {UserId} has {LoginAttempts} failed login attempts but was not blocked", userId, user.LoginAttempts);
            }

            await _userManager.UpdateAsync(user);
        }

        public async Task RecordSuccessfulLoginAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LoginAttempts = 0;
            user.LastLoginDate = DateTime.UtcNow;
            user.LastLoginAttempt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
        }

        public async Task BlockUserAsync(string userId, string reason, DateTime? blockedUntil = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            // Check if user is admin - prevent blocking admin accounts
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                _logger.LogWarning("Attempted to block admin user {UserId} but operation was prevented", userId);
                return; // Don't block admin users
            }

            user.IsBlocked = true;
            user.BlockReason = reason;
            user.BlockedUntil = blockedUntil ?? DateTime.UtcNow.AddHours(24);

            await _userManager.UpdateAsync(user);
            _logger.LogInformation("User {UserId} blocked until {BlockedUntil} for reason: {Reason}", userId, user.BlockedUntil, reason);
        }

        public async Task UnblockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.IsBlocked = false;
            user.BlockedUntil = null;
            user.BlockReason = null;
            user.LoginAttempts = 0;

            await _userManager.UpdateAsync(user);
            _logger.LogInformation("User {UserId} unblocked", userId);
        }

        public async Task<List<ApplicationUser>> GetBlockedUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsBlocked)
                .OrderByDescending(u => u.BlockedUntil)
                .ToListAsync();
        }

        public async Task<int> GetFailedLoginAttemptsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.LoginAttempts ?? 0;
        }

        public async Task<DateTime?> GetBlockExpiryAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.BlockedUntil;
        }

        public async Task<int> GetRemainingAttemptsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return 5; // Default attempts for non-existent users
            
            const int maxAttempts = 5;
            return Math.Max(0, maxAttempts - user.LoginAttempts);
        }

        public async Task<DateTime?> GetLoginAttemptsResetTimeAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.LastLoginAttempt == null || user.LoginAttempts == 0) 
                return null;
            
            return user.LastLoginAttempt.Value.AddMinutes(30);
        }

        public async Task UnblockAllAdminAccountsAsync()
        {
            var blockedAdmins = await _context.Users
                .Where(u => u.IsBlocked)
                .ToListAsync();

            foreach (var user in blockedAdmins)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    user.IsBlocked = false;
                    user.BlockedUntil = null;
                    user.BlockReason = null;
                    user.LoginAttempts = 0;
                    
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Admin user {UserId} ({UserName}) was unblocked", user.Id, user.UserName);
                }
            }
        }
    }
}
