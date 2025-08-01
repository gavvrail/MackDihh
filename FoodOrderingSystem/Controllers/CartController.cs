using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public CartController(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuItem)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId ?? string.Empty };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Clean up any duplicate cart items (merge them)
                await CleanupDuplicateCartItems(cart.Id);
                
                // Reload cart after cleanup
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            return View(cart);
        }

        private async Task CleanupDuplicateCartItems(int cartId)
        {
            // Get all cart items for this cart
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();

            // Group by MenuItemId and merge duplicates
            var groupedItems = cartItems.GroupBy(ci => ci.MenuItemId).ToList();

            foreach (var group in groupedItems)
            {
                if (group.Count() > 1)
                {
                    var items = group.ToList();
                    var firstItem = items.First();
                    
                    // Sum up all quantities
                    var totalQuantity = items.Sum(ci => ci.Quantity);
                    firstItem.Quantity = totalQuantity;
                    
                    // Remove duplicate items
                    for (int i = 1; i < items.Count; i++)
                    {
                        _context.CartItems.Remove(items[i]);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        // ... (The AddToCart method remains the same) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int menuItemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Unauthorized();

                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem == null) return NotFound();

                // Get or create cart with proper loading
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    
                    // Reload cart to ensure proper navigation properties
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                }

                // Check for existing cart item using direct database query
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.MenuItemId == menuItemId);

                if (existingCartItem != null)
                {
                    // Update existing item quantity
                    existingCartItem.Quantity++;
                    _context.CartItems.Update(existingCartItem);
                }
                else
                {
                    // Create new cart item
                    var newCartItem = new CartItem
                    {
                        MenuItemId = menuItemId,
                        CartId = cart.Id,
                        Quantity = 1
                    };
                    _context.CartItems.Add(newCartItem);
                }

                await _context.SaveChangesAsync();
                var newCount = await _cartService.GetCartItemCountAsync();
                return Json(new { success = true, message = $"{menuItem.Name} added to cart!", newCount = newCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while adding item to cart." });
            }
        }

        // --- NEW METHODS FOR UPDATING AND REMOVING ITEMS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null) return NotFound();

            if (quantity > 0)
            {
                cartItem.Quantity = quantity;
            }
            else
            {
                _context.CartItems.Remove(cartItem);
            }

            await _context.SaveChangesAsync();
            return await GetCartSummary();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return await GetCartSummary();
        }

        // GET: /Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuItem)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "Checkout");
        }

        private async Task<JsonResult> GetCartSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuItem)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            decimal subtotal = cart?.CartItems.Sum(item => item.Quantity * item.MenuItem.Price) ?? 0;
            int newCount = await _cartService.GetCartItemCountAsync();

            return Json(new { success = true, subtotal = subtotal.ToString("C"), newCount });
        }
    }
}
