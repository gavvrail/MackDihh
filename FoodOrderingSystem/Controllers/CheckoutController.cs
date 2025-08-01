using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;

        public CheckoutController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
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
                
                if (!ModelState.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("ModelState is invalid");
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

                // Create order items
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
                    System.Diagnostics.Debug.WriteLine($"Added order item: {cartItem.MenuItem.Name} x {cartItem.Quantity}");
                }

                // Clear cart
                _context.CartItems.RemoveRange(cart.CartItems);

                // Save all changes to database
                System.Diagnostics.Debug.WriteLine("Saving to database...");
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Database save completed successfully");

                // Send confirmation email (don't let email failure stop the order)
                var user = await _context.Users.FindAsync(userId);
                if (user?.Email != null)
                {
                    try
                    {
                        var emailBody = EmailTemplates.GetOrderConfirmationTemplate(
                            order.OrderNumber, 
                            user.UserName ?? "Customer", 
                            order.Total, 
                            order.DeliveryAddress ?? "N/A", 
                            order.OrderDate);
                        
                        await _emailSender.SendEmailAsync(user.Email, 
                            $"Order Confirmation - {order.OrderNumber}", 
                            emailBody);
                        System.Diagnostics.Debug.WriteLine("Email sent successfully");
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail the order
                        // The order is already saved, so we just log the email failure
                        System.Diagnostics.Debug.WriteLine($"Email sending failed: {emailEx.Message}");
                    }
                }

                // Success - redirect to confirmation page
                System.Diagnostics.Debug.WriteLine("Order placement completed successfully");
                TempData["SuccessMessage"] = $"Order placed successfully! Order number: {order.OrderNumber}";
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
        
        [StringLength(500, ErrorMessage = "Delivery instructions cannot be longer than 500 characters")]
        public string DeliveryInstructions { get; set; } = "";
        
        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters")]
        public string Notes { get; set; } = "";
    }
} 