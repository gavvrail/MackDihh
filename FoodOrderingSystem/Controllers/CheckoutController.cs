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
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please log in to proceed with checkout.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                var user = await _context.Users.FindAsync(userId);
                var checkoutViewModel = new CheckoutViewModel
                {
                    Cart = cart,
                    DeliveryAddress = user?.Address ?? "",
                    CustomerPhone = user?.PhoneNumber ?? "",
                    DeliveryInstructions = "",
                    Notes = ""
                };

                // Show helpful message if user has saved information
                if (!string.IsNullOrEmpty(user?.Address) || !string.IsNullOrEmpty(user?.PhoneNumber))
                {
                    TempData["InfoMessage"] = "Your saved delivery information has been pre-filled. You can modify it if needed.";
                }

                return View(checkoutViewModel);
            }
            catch (Exception)
            {
                // Log the exception here if you have logging configured
                TempData["ErrorMessage"] = "An error occurred while loading the checkout page. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // POST: /Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting Order Placement ===");
                
                // Manual validation for required fields only
                if (string.IsNullOrWhiteSpace(model.DeliveryAddress))
                {
                    TempData["ErrorMessage"] = "Delivery address is required.";
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var currentCart = await _context.Carts
                        .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.MenuItem)
                        .FirstOrDefaultAsync(c => c.UserId == currentUserId);
                    model.Cart = currentCart ?? new Cart();
                    return View("Index", model);
                }
                
                if (string.IsNullOrWhiteSpace(model.CustomerPhone))
                {
                    TempData["ErrorMessage"] = "Phone number is required.";
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var currentCart = await _context.Carts
                        .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.MenuItem)
                        .FirstOrDefaultAsync(c => c.UserId == currentUserId);
                    model.Cart = currentCart ?? new Cart();
                    return View("Index", model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");
                
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please log in to proceed with checkout.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Cart is empty or null");
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                System.Diagnostics.Debug.WriteLine($"Cart has {cart.CartItems.Count} items");

                // Calculate totals
                var subtotal = cart.CartItems.Sum(item => item.Quantity * item.MenuItem.Price);
                var tax = subtotal * 0.06m; // 6% tax
                var deliveryFee = subtotal >= 100 ? 0 : 5.00m; // Free delivery for orders over RM100
                var total = subtotal + tax + deliveryFee;

                System.Diagnostics.Debug.WriteLine($"Subtotal: {subtotal}, Tax: {tax}, Delivery: {deliveryFee}, Total: {total}");

                // Save user information to profile for future use
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    // Update user's address and phone if provided
                    if (!string.IsNullOrWhiteSpace(model.DeliveryAddress))
                    {
                        user.Address = model.DeliveryAddress.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(model.CustomerPhone))
                    {
                        user.PhoneNumber = model.CustomerPhone.Trim();
                    }
                    await _context.SaveChangesAsync();
                }

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45), // 45 minutes delivery time
                    Subtotal = subtotal,
                    Tax = tax,
                    DeliveryFee = deliveryFee,
                    Total = total,
                    Status = OrderStatus.Pending,
                    DeliveryAddress = model.DeliveryAddress?.Trim(),
                    DeliveryInstructions = model.DeliveryInstructions?.Trim(),
                    CustomerPhone = model.CustomerPhone?.Trim(),
                    Notes = model.Notes?.Trim()
                };

                System.Diagnostics.Debug.WriteLine($"Created order with number: {order.OrderNumber}");

                _context.Orders.Add(order);

                // Save the order first to get the generated ID
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Order saved with ID: {order.Id}");

                // Create order items with the correct OrderId
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = cartItem.MenuItemId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.MenuItem.Price
                    };
                    _context.OrderItems.Add(orderItem);
                    System.Diagnostics.Debug.WriteLine($"Added order item: {cartItem.MenuItem.Name} x {cartItem.Quantity} for Order ID: {order.Id}");
                }

                // Clear cart completely
                System.Diagnostics.Debug.WriteLine($"Clearing {cart.CartItems.Count} items from cart...");
                
                // Award points to user based on items ordered (BEFORE clearing cart)
                int totalPointsEarned = 0;
                if (user != null)
                {
                    foreach (var cartItem in cart.CartItems)
                    {
                        // Calculate points based on PointsPerItem field from menu item
                        int pointsForThisItem = cartItem.MenuItem.PointsPerItem * cartItem.Quantity;
                        totalPointsEarned += pointsForThisItem;
                    }

                    if (totalPointsEarned > 0)
                    {
                        user.Points += totalPointsEarned;
                        user.TotalPointsEarned += totalPointsEarned;

                        // Create points transaction record
                        var pointsTransaction = new UserPointsTransaction
                        {
                            UserId = userId,
                            Points = totalPointsEarned,
                            Type = PointsTransactionType.Earned,
                            Description = $"Order #{order.OrderNumber} - {totalPointsEarned} points earned",
                            OrderId = order.Id
                        };

                        _context.UserPointsTransactions.Add(pointsTransaction);
                        System.Diagnostics.Debug.WriteLine($"Awarded {totalPointsEarned} points to user {userId}");
                    }
                }
                
                _context.CartItems.RemoveRange(cart.CartItems);
                
                // Also remove the cart itself if it's empty
                if (!cart.CartItems.Any())
                {
                    _context.Carts.Remove(cart);
                    System.Diagnostics.Debug.WriteLine("Removed empty cart");
                }

                // Save order items and cart changes
                System.Diagnostics.Debug.WriteLine("Saving order items and clearing cart...");
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Database save completed successfully - Order items saved and cart cleared!");

                // Email sending removed for instant order completion

                // Success - redirect to confirmation page
                System.Diagnostics.Debug.WriteLine("Order placement completed successfully");
                
                // Show points earned message
                if (totalPointsEarned > 0)
                {
                    TempData["SuccessMessage"] = $"Order placed successfully! You earned {totalPointsEarned} points from this order.";
                }
                
                // Don't set TempData message here - the confirmation page itself shows success
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Order placement error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "An error occurred while placing your order. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: /Checkout/Confirmation/{orderId}
        public async Task<IActionResult> Confirmation(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }

    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = null!;
        
        [Required(ErrorMessage = "Delivery address is required")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string DeliveryAddress { get; set; } = "";
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string CustomerPhone { get; set; } = "";
        
        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }
        
        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }
    }
} 