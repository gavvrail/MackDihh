using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CheckoutController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

                // Get cart with items
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.MenuItem)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add items before proceeding to checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Ensure all cart items have valid MenuItems
                cart.CartItems = cart.CartItems.Where(ci => ci.MenuItem != null).ToList();
                
                if (!cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart contains invalid items. Please add items before proceeding to checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get user info
                var user = await _context.Users.FindAsync(userId);

                var checkoutViewModel = new ViewModels.CheckoutViewModel
                {
                    Cart = cart,
                    DeliveryAddress = user?.Address ?? "",
                    CustomerPhone = user?.PhoneNumber ?? "",
                    DeliveryInstructions = "",
                    Notes = "",
                    PaymentMethod = "Cash" // Default to Cash
                };

                return View(checkoutViewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading the checkout page: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        // POST: /Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(ViewModels.CheckoutViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Your session has expired. Please log in again.";
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

                if (!ModelState.IsValid)
                {
                    // Re-populate the cart data for the view
                    model.Cart = cart;
                    TempData["ErrorMessage"] = "Please correct the errors below.";
                    return View("Index", model);
                }

                // Calculate totals with validation
                var subtotal = cart.CartItems.Sum(item => 
                {
                    if (item.MenuItem?.Price == null || item.MenuItem.Price < 0)
                    {
                        throw new InvalidOperationException($"Invalid price for menu item: {item.MenuItem?.Name ?? "Unknown"}");
                    }
                    if (item.Quantity <= 0)
                    {
                        throw new InvalidOperationException($"Invalid quantity for menu item: {item.MenuItem.Name}");
                    }
                    return item.Quantity * item.MenuItem.Price;
                });
                
                if (subtotal <= 0)
                {
                    throw new InvalidOperationException("Invalid order total calculated.");
                }
                
                // Get order settings from configuration
                var taxRate = decimal.Parse(_configuration["OrderSettings:TaxRate"] ?? "0.06");
                var deliveryFee = decimal.Parse(_configuration["OrderSettings:DeliveryFee"] ?? "5.00");
                var freeDeliveryThreshold = decimal.Parse(_configuration["OrderSettings:FreeDeliveryThreshold"] ?? "100.00");
                var estimatedDeliveryMinutes = int.Parse(_configuration["OrderSettings:EstimatedDeliveryMinutes"] ?? "45");
                
                var tax = subtotal * taxRate;
                var deliveryFeeAmount = subtotal >= freeDeliveryThreshold ? 0 : deliveryFee;
                
                // Apply promo code discount if provided
                decimal discountAmount = 0;
                string? appliedPromoCode = null;
                
                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    // Check if it's a valid deal promo code
                    var deal = await _context.Deals
                        .FirstOrDefaultAsync(d => d.PromoCode == model.PromoCode &&
                                                  d.IsActive &&
                                                  d.StartDate <= DateTime.UtcNow &&
                                                  d.EndDate >= DateTime.UtcNow &&
                                                  d.CurrentUses < d.MaxUses);

                    if (deal != null && subtotal >= deal.MinimumOrderAmount)
                    {
                        if (deal.DiscountPercentage > 0)
                        {
                            discountAmount = subtotal * (deal.DiscountPercentage / 100);
                        }
                        else if (deal.DiscountedPrice > 0)
                        {
                            discountAmount = deal.DiscountedPrice;
                        }
                        
                        appliedPromoCode = model.PromoCode;
                        
                        // Update deal usage count
                        deal.CurrentUses++;
                    }
                }
                
                var total = subtotal + tax + deliveryFeeAmount - discountAmount;

                // Declare variables outside transaction scope
                Order order;
                ApplicationUser? user = null;

                // Use database transaction to ensure data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create order
                    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks % 10000:D4}";
                    order = new Order
                    {
                        UserId = userId,
                        OrderNumber = orderNumber,
                        OrderDate = DateTime.UtcNow,
                        EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(estimatedDeliveryMinutes),
                        Subtotal = subtotal,
                        Tax = tax,
                        DeliveryFee = deliveryFeeAmount,
                        Total = total,
                        TotalAmount = total,
                        Status = OrderStatus.Pending,
                        DeliveryAddress = model.DeliveryAddress.Trim(),
                        DeliveryInstructions = model.DeliveryInstructions?.Trim(),
                        CustomerPhone = model.CustomerPhone.Trim(),
                        PhoneNumber = model.CustomerPhone.Trim(),
                        Notes = model.Notes?.Trim(),
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = model.PaymentMethod == "Cash" ? "Pending" : "Paid",
                        PromoCode = appliedPromoCode,
                        DiscountAmount = discountAmount
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Add order items
                    foreach (var cartItem in cart.CartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            MenuItemId = cartItem.MenuItemId,
                            Quantity = cartItem.Quantity,
                            Price = cartItem.MenuItem.Price,
                            UnitPrice = cartItem.MenuItem.Price
                        };
                        _context.OrderItems.Add(orderItem);
                    }

                    // Add points earning for all users
                    user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Users earn 1 point for every RM1 spent
                        int pointsEarned = (int)Math.Floor(total);
                        if (pointsEarned > 0)
                        {
                            user.Points += pointsEarned;
                            user.TotalPointsEarned += pointsEarned;

                            // Create points transaction record
                            var pointsTransaction = new UserPointsTransaction
                            {
                                UserId = userId,
                                Points = pointsEarned,
                                Type = PointsTransactionType.Earned,
                                Description = $"Order #{orderNumber} - Earned {pointsEarned} points"
                            };

                            _context.UserPointsTransactions.Add(pointsTransaction);
                        }
                    }

                    // Clear cart
                    _context.CartItems.RemoveRange(cart.CartItems);

                    // Commit all changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                var pointsMessage = "";
                if (user != null)
                {
                    var pointsEarned = (int)Math.Floor(total);
                    pointsMessage = $" You earned {pointsEarned} points!";
                }

                TempData["SuccessMessage"] = $"Order placed successfully!{pointsMessage}";
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while placing your order. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: /Checkout/Test - Simple test to verify routing
        public IActionResult Test()
        {
            return Content("Checkout controller is working!");
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
}
