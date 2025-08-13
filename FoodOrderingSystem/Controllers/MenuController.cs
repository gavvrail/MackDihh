// This using statement is now correct
using FoodOrderingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                                           .Include(c => c.MenuItems)
                                           .ToListAsync();

            return View(categories);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RedeemItem(int menuItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var user = await _context.Users.FindAsync(userId);
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);

            if (user == null || menuItem == null)
            {
                return NotFound();
            }

            // Calculate points required based on PointsPerItem field
            int pointsRequired = Math.Max(1, menuItem.PointsPerItem);

            if (user.Points < pointsRequired)
            {
                TempData["ErrorMessage"] = $"You need {pointsRequired} points to redeem this item. You have {user.Points} points.";
                return RedirectToAction("Index", "Deals");
            }

            // Deduct points from user
            user.Points -= pointsRequired;
            user.TotalPointsRedeemed += pointsRequired;

            // Create points transaction record
            var pointsTransaction = new UserPointsTransaction
            {
                UserId = userId,
                Points = -pointsRequired,
                Type = PointsTransactionType.Redeemed,
                Description = $"Redeemed: {menuItem.Name} for {pointsRequired} points"
            };

            // Create redemption record
            var redemptionCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            var userRedemption = new UserRedemption
            {
                UserId = userId,
                PointsRewardId = 0, // We'll use 0 for direct menu item redemptions
                PointsSpent = pointsRequired,
                RedeemedAt = DateTime.UtcNow,
                RedemptionCode = redemptionCode
            };

            _context.UserPointsTransactions.Add(pointsTransaction);
            _context.UserRedemptions.Add(userRedemption);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully redeemed {menuItem.Name}! Your redemption code is: {redemptionCode}. Show this code to claim your free item.";
            return RedirectToAction("Index", "Deals");
        }
    }
}
