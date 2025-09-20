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
                // Only clean up duplicates if there are more than 10 items (performance optimization)
                if (cart.CartItems.Count > 10)
                {
                    await CleanupDuplicateCartItems(cart.Id);
                    
                    // Reload cart after cleanup
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.MenuItem)
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                }
                
                // Ensure cart is not null after cleanup
                if (cart == null)
                {
                    cart = new Cart { UserId = userId ?? string.Empty };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
            }

            return View(cart);
        }

        private async Task CleanupDuplicateCartItems(int cartId)
        {
            try
            {
                // Get all cart items for this cart with a single query
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .ToListAsync();

                // Group by MenuItemId and merge duplicates efficiently
                var groupedItems = cartItems
                    .GroupBy(ci => ci.MenuItemId)
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (groupedItems.Any())
                {
                    var itemsToRemove = new List<CartItem>();

                    foreach (var group in groupedItems)
                    {
                        var items = group.ToList();
                        var firstItem = items.First();
                        
                        // Sum up all quantities
                        var totalQuantity = items.Sum(ci => ci.Quantity);
                        firstItem.Quantity = totalQuantity;
                        
                        // Mark duplicate items for removal
                        itemsToRemove.AddRange(items.Skip(1));
                    }

                    // Remove all duplicate items in a single operation
                    if (itemsToRemove.Any())
                    {
                        _context.CartItems.RemoveRange(itemsToRemove);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw to prevent cart access failure
                Console.WriteLine($"Error cleaning up duplicate cart items: {ex.Message}");
            }
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
                if (cart == null)
                {
                    return Json(new { success = false, message = "Cart not found." });
                }
                
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
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding item to cart." });
            }
        }

        // --- NEW METHODS FOR UPDATING AND REMOVING ITEMS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItem = await _context.CartItems
                    .Include(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem == null) return NotFound();

                using var transaction = await _context.Database.BeginTransactionAsync();

                if (quantity > 0)
                {
                    // Handle quantity change for redeemed items
                    if (cartItem.IsRedeemedWithPoints)
                    {
                        var originalQuantity = cartItem.Quantity;
                        var quantityDifference = quantity - originalQuantity;
                        
                        if (quantityDifference != 0)
                        {
                            var user = await _context.Users.FindAsync(userId);
                            if (user != null)
                            {
                                // Calculate points per item
                                var pointsPerItem = cartItem.PointsUsed / originalQuantity;
                                var pointsChange = pointsPerItem * quantityDifference;

                                if (quantityDifference > 0)
                                {
                                    // Increasing quantity - deduct more points
                                    if (user.Points >= pointsChange)
                                    {
                                        user.Points -= pointsChange;
                                        user.TotalPointsRedeemed += pointsChange;
                                        cartItem.PointsUsed += pointsChange;

                                        // Create transaction record
                                        var transaction_record = new UserPointsTransaction
                                        {
                                            UserId = userId,
                                            Points = -pointsChange,
                                            Type = PointsTransactionType.Redeemed,
                                            Description = $"Additional redemption: {cartItem.MenuItem.Name} (qty +{quantityDifference}) - {pointsChange} points"
                                        };
                                        _context.UserPointsTransactions.Add(transaction_record);
                                    }
                                    else
                                    {
                                        await transaction.RollbackAsync();
                                        return Json(new { success = false, message = $"Not enough points. You need {pointsChange} more points to increase quantity." });
                                    }
                                }
                                else
                                {
                                    // Decreasing quantity - refund points
                                    var pointsToRefund = Math.Abs(pointsChange);
                                    user.Points += pointsToRefund;
                                    user.TotalPointsRedeemed -= pointsToRefund;
                                    cartItem.PointsUsed -= pointsToRefund;

                                    // Create refund transaction record
                                    var refundTransaction = new UserPointsTransaction
                                    {
                                        UserId = userId,
                                        Points = pointsToRefund,
                                        Type = PointsTransactionType.Refunded,
                                        Description = $"Quantity refund: {cartItem.MenuItem.Name} (qty {quantityDifference}) - {pointsToRefund} points refunded"
                                    };
                                    _context.UserPointsTransactions.Add(refundTransaction);
                                }
                            }
                        }
                    }

                    cartItem.Quantity = quantity;
                }
                else
                {
                    // Quantity is 0, remove the item (this will call the same logic as RemoveItem)
                    if (cartItem.IsRedeemedWithPoints && cartItem.PointsUsed > 0)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            // Refund all points for this item
                            user.Points += cartItem.PointsUsed;
                            user.TotalPointsRedeemed -= cartItem.PointsUsed;

                            // Create refund transaction record
                            var refundTransaction = new UserPointsTransaction
                            {
                                UserId = userId,
                                Points = cartItem.PointsUsed,
                                Type = PointsTransactionType.Refunded,
                                Description = $"Refund: Removed {cartItem.MenuItem.Name} from cart - {cartItem.PointsUsed} points refunded"
                            };
                            _context.UserPointsTransactions.Add(refundTransaction);
                        }
                    }

                    _context.CartItems.Remove(cartItem);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetCartSummary();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the cart." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItem = await _context.CartItems
                    .Include(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem != null)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Check if this is a redeemed item and refund points
                    if (cartItem.IsRedeemedWithPoints && cartItem.PointsUsed > 0)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            // Calculate points to refund (points per quantity)
                            var pointsToRefund = cartItem.PointsUsed;
                            
                            // Refund points to user
                            user.Points += pointsToRefund;
                            user.TotalPointsRedeemed -= pointsToRefund;

                            // Create refund transaction record
                            var refundTransaction = new UserPointsTransaction
                            {
                                UserId = userId,
                                Points = pointsToRefund,
                                Type = PointsTransactionType.Refunded,
                                Description = $"Refund: Removed {cartItem.MenuItem.Name} from cart - {pointsToRefund} points refunded"
                            };
                            _context.UserPointsTransactions.Add(refundTransaction);

                            // Remove the redemption record if it exists
                            var redemptionRecord = await _context.UserRedemptions
                                .FirstOrDefaultAsync(ur => ur.UserId == userId && 
                                                          ur.MenuItemId == cartItem.MenuItemId && 
                                                          ur.RedemptionCode == cartItem.RedemptionCode);
                            if (redemptionRecord != null)
                            {
                                _context.UserRedemptions.Remove(redemptionRecord);
                            }
                        }
                    }

                    // Remove the cart item
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { 
                        success = true, 
                        message = cartItem.IsRedeemedWithPoints ? 
                            $"Item removed and {cartItem.PointsUsed} points refunded to your account." : 
                            "Item removed from cart."
                    });
                }

                return Json(new { success = false, message = "Item not found in cart." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while removing the item." });
            }
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

            return Json(new { success = true, subtotal = "RM " + subtotal.ToString("F2"), newCount });
        }
    }
}

