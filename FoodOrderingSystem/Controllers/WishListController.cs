using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class WishListController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WishListController> _logger;

        public WishListController(ApplicationDbContext context, ILogger<WishListController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /WishList
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishListItems = await _context.WishListItems
                .Include(w => w.MenuItem)
                .ThenInclude(m => m.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();

            return View(wishListItems);
        }

        // POST: /WishList/AddToWishList
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishList(int menuItemId, string notes = "")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Check if item already exists in wish list
                var existingItem = await _context.WishListItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MenuItemId == menuItemId);

                if (existingItem != null)
                {
                    return Json(new { success = false, message = "Item is already in your wish list" });
                }

                // Check if menu item exists
                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item not found" });
                }

                var wishListItem = new WishListItem
                {
                    UserId = userId,
                    MenuItemId = menuItemId,
                    Notes = notes,
                    AddedDate = DateTime.UtcNow,
                    IsAvailable = menuItem.IsAvailable
                };

                _context.WishListItems.Add(wishListItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added menu item {MenuItemId} to wish list", userId, menuItemId);

                return Json(new { success = true, message = $"{menuItem.Name} added to wish list!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to wish list");
                return Json(new { success = false, message = "An error occurred while adding item to wish list" });
            }
        }

        // POST: /WishList/RemoveFromWishList
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishList(int wishListItemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishListItem = await _context.WishListItems
                    .Include(w => w.MenuItem)
                    .FirstOrDefaultAsync(w => w.Id == wishListItemId && w.UserId == userId);

                if (wishListItem == null)
                {
                    return Json(new { success = false, message = "Wish list item not found" });
                }

                var itemName = wishListItem.MenuItem?.Name ?? "Item";
                _context.WishListItems.Remove(wishListItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed menu item {MenuItemId} from wish list", userId, wishListItem.MenuItemId);

                return Json(new { success = true, message = $"{itemName} removed from wish list" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from wish list");
                return Json(new { success = false, message = "An error occurred while removing item from wish list" });
            }
        }

        // POST: /WishList/MoveToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int wishListItemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishListItem = await _context.WishListItems
                    .Include(w => w.MenuItem)
                    .FirstOrDefaultAsync(w => w.Id == wishListItemId && w.UserId == userId);

                if (wishListItem == null)
                {
                    return Json(new { success = false, message = "Wish list item not found" });
                }

                // Check if menu item is available
                if (!wishListItem.MenuItem.IsAvailable)
                {
                    return Json(new { success = false, message = "Item is currently unavailable" });
                }

                // Get or create cart
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Check if item already exists in cart
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.MenuItemId == wishListItem.MenuItemId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity++;
                    _context.CartItems.Update(existingCartItem);
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        MenuItemId = wishListItem.MenuItemId,
                        CartId = cart.Id,
                        Quantity = 1
                    };
                    _context.CartItems.Add(cartItem);
                }

                // Remove from wish list
                _context.WishListItems.Remove(wishListItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} moved menu item {MenuItemId} from wish list to cart", userId, wishListItem.MenuItemId);

                return Json(new { success = true, message = $"{wishListItem.MenuItem.Name} moved to cart!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving item from wish list to cart");
                return Json(new { success = false, message = "An error occurred while moving item to cart" });
            }
        }

        // POST: /WishList/UpdateNotes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotes(int wishListItemId, string notes)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishListItem = await _context.WishListItems
                    .FirstOrDefaultAsync(w => w.Id == wishListItemId && w.UserId == userId);

                if (wishListItem == null)
                {
                    return Json(new { success = false, message = "Wish list item not found" });
                }

                wishListItem.Notes = notes;
                _context.WishListItems.Update(wishListItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Notes updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wish list item notes");
                return Json(new { success = false, message = "An error occurred while updating notes" });
            }
        }

        // POST: /WishList/UpdatePriority
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(int wishListItemId, int priority)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (priority < 1 || priority > 3)
                {
                    return Json(new { success = false, message = "Priority must be between 1 and 3" });
                }

                var wishListItem = await _context.WishListItems
                    .FirstOrDefaultAsync(w => w.Id == wishListItemId && w.UserId == userId);

                if (wishListItem == null)
                {
                    return Json(new { success = false, message = "Wish list item not found" });
                }

                wishListItem.Priority = priority;
                _context.WishListItems.Update(wishListItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Priority updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wish list item priority");
                return Json(new { success = false, message = "An error occurred while updating priority" });
            }
        }

        // GET: /WishList/GetWishListCount
        [HttpGet]
        public async Task<IActionResult> GetWishListCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.WishListItems
                    .CountAsync(w => w.UserId == userId);

                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wish list count");
                return Json(new { count = 0 });
            }
        }

        // POST: /WishList/GetWishListItemId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetWishListItemId(int menuItemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishListItem = await _context.WishListItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MenuItemId == menuItemId);

                if (wishListItem == null)
                {
                    return Json(new { success = false, message = "Item not found in wish list" });
                }

                return Json(new { success = true, wishListItemId = wishListItem.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wish list item ID");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: /WishList/ClearWishList
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWishList()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishListItems = await _context.WishListItems
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                if (!wishListItems.Any())
                {
                    return Json(new { success = false, message = "Wish list is already empty" });
                }

                _context.WishListItems.RemoveRange(wishListItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} cleared their wish list", userId);

                return Json(new { success = true, message = "Wish list cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing wish list");
                return Json(new { success = false, message = "An error occurred while clearing wish list" });
            }
        }
    }
}
