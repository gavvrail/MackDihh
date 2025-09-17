// This using statement is now correct
using FoodOrderingSystem.Data;
using FoodOrderingSystem.ViewModels;
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

        public async Task<IActionResult> Index(string sortBy = "name")
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable);

            // Apply sorting
            switch (sortBy.ToLower())
            {
                case "price-low":
                    query = query.OrderBy(m => m.Price);
                    break;
                case "price-high":
                    query = query.OrderByDescending(m => m.Price);
                    break;
                case "rating":
                    query = query.OrderByDescending(m => m.AverageRating);
                    break;
                case "popular":
                    query = query.OrderByDescending(m => m.TotalReviews);
                    break;
                case "newest":
                    query = query.OrderByDescending(m => m.CreatedDate);
                    break;
                case "name":
                default:
                    query = query.OrderBy(m => m.Name);
                    break;
            }

            var menuItems = await query.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            // Get user's wishlist items if authenticated
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItemIds = new HashSet<int>();
            
            if (!string.IsNullOrEmpty(userId))
            {
                var wishlistIds = await _context.WishListItems
                    .Where(w => w.UserId == userId)
                    .Select(w => w.MenuItemId)
                    .ToListAsync();
                wishlistItemIds = wishlistIds.ToHashSet();
            }

            var viewModel = new MenuViewModel
            {
                MenuItems = menuItems,
                Categories = categories,
                SelectedSortBy = sortBy,
                SelectedCategory = "",
                WishlistItemIds = wishlistItemIds
            };

            return View(viewModel);
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

            // Check if user has enough points
            if (user.Points < 1)
            {
                TempData["ErrorMessage"] = "You need at least 1 point to redeem items. Start earning points by placing orders!";
                return RedirectToAction("Index", "Deals");
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
                MenuItemId = menuItemId, // Link to the specific menu item
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
