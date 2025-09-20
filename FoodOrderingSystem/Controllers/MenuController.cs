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

            // Calculate points required (use PointsPerItem if set, otherwise use price as points)
            int pointsRequired = menuItem.PointsPerItem > 0 ? menuItem.PointsPerItem : (int)Math.Ceiling(menuItem.Price);

            // Check if user has enough points
            if (user.Points < pointsRequired)
            {
                TempData["ErrorMessage"] = $"You need {pointsRequired} points to redeem this item. You have {user.Points} points.";
                return RedirectToAction("Index", "Deals");
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

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
                _context.UserPointsTransactions.Add(pointsTransaction);

                // Generate redemption code
                var redemptionCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // Get or create user's cart
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync(); // Save to get cart ID
                }

                // Check if this item is already in cart as a redeemed item
                var existingCartItem = cart.CartItems.FirstOrDefault(ci => 
                    ci.MenuItemId == menuItemId && ci.IsRedeemedWithPoints);

                if (existingCartItem != null)
                {
                    // Increase quantity of existing redeemed item
                    existingCartItem.Quantity += 1;
                    existingCartItem.PointsUsed += pointsRequired;
                }
                else
                {
                    // Add new redeemed item to cart
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        MenuItemId = menuItemId,
                        Quantity = 1,
                        IsRedeemedWithPoints = true,
                        PointsUsed = pointsRequired,
                        RedemptionCode = redemptionCode
                    };
                    _context.CartItems.Add(cartItem);
                }

                // Create redemption record for tracking
                var userRedemption = new UserRedemption
                {
                    UserId = userId,
                    MenuItemId = menuItemId,
                    PointsSpent = pointsRequired,
                    RedeemedAt = DateTime.UtcNow,
                    RedemptionCode = redemptionCode
                };
                _context.UserRedemptions.Add(userRedemption);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Successfully redeemed {menuItem.Name} for {pointsRequired} points! Item added to your cart as FREE. Proceed to checkout to complete your order.";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while redeeming the item. Please try again.";
                return RedirectToAction("Index", "Deals");
            }
        }
    }
}
