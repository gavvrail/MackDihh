using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace FoodOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LoginSecurityService _loginSecurityService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, LoginSecurityService loginSecurityService, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _loginSecurityService = loginSecurityService;
            _logger = logger;
        }

        // GET: /Admin (Dashboard)
        public async Task<IActionResult> Index()
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && successMessage.Contains("Order placed"))
            {
                TempData.Remove("SuccessMessage");
            }
            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalSales = await _context.Orders.SumAsync(o => o.Total),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync(),
                TopSellingItems = await _context.OrderItems
                    .Include(oi => oi.MenuItem)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g => new TopSellingItem
                    {
                        MenuItemName = g.First().MenuItem.Name,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(5)
                    .ToListAsync()
            };

            return View(dashboardViewModel);
        }

        // Debug action to check admin user status (remove this in production)
        [AllowAnonymous]
        public async Task<IActionResult> DebugAdmin()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@mackdihh.com");
            var adminUserByUsername = await _userManager.FindByNameAsync("admin");
            
            var result = new
            {
                AdminByEmail = adminUser != null ? new { adminUser.Id, adminUser.UserName, adminUser.Email, adminUser.EmailConfirmed } : null,
                AdminByUsername = adminUserByUsername != null ? new { adminUserByUsername.Id, adminUserByUsername.UserName, adminUserByUsername.Email, adminUserByUsername.EmailConfirmed } : null,
                IsInAdminRole = adminUser != null ? await _userManager.IsInRoleAsync(adminUser, "Admin") : false,
                IsInAdminRoleByUsername = adminUserByUsername != null ? await _userManager.IsInRoleAsync(adminUserByUsername, "Admin") : false,
                TotalUsers = await _context.Users.CountAsync(),
                TotalMenuItems = await _context.MenuItems.CountAsync()
            };
            
            return Json(result);
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Orders(string status = "", string search = "", string current = "")
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && successMessage.Contains("Order placed"))
            {
                TempData.Remove("SuccessMessage");
            }
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .AsQueryable();

            // Handle "current" parameter to show all orders except Delivered and Cancelled
            if (!string.IsNullOrEmpty(current) && current.ToLower() == "true")
            {
                query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
            }
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => 
                    o.OrderNumber.Contains(search) || 
                    (o.User != null && o.User.UserName != null && o.User.UserName.Contains(search)) ||
                    (o.DeliveryAddress != null && o.DeliveryAddress.Contains(search)));
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentFilter = current;
            return View(orders);
        }

        // GET: /Admin/GetOrderDetails/{orderId}
        [HttpGet]
        [Route("Admin/GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Looking for order with ID: {orderId}");
                
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Order with ID {orderId} not found");
                    
                    // Check if any orders exist at all
                    var allOrders = await _context.Orders.Select(o => new { o.Id, o.OrderNumber }).ToListAsync();
                    var ordersList = string.Join(", ", allOrders.Select(o => $"ID:{o.Id}({o.OrderNumber})"));
                    
                    return Json(new { 
                        success = false, 
                        message = $"Order not found. Available orders: {ordersList}" 
                    });
                }

                var orderData = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    customerName = order.User?.UserName ?? "Unknown",
                    orderDate = order.OrderDate,
                    status = order.Status.ToString(),
                    total = order.Total,
                    subtotal = order.Subtotal,
                    tax = order.Tax,
                    deliveryFee = order.DeliveryFee,
                    deliveryAddress = order.DeliveryAddress,
                    customerPhone = order.CustomerPhone,
                    deliveryInstructions = order.DeliveryInstructions,
                    notes = order.Notes,
                    estimatedDeliveryTime = order.EstimatedDeliveryTime,
                    actualDeliveryTime = order.ActualDeliveryTime,
                    orderItems = order.OrderItems.Select(oi => new
                    {
                        menuItemName = oi.MenuItem.Name,
                        quantity = oi.Quantity,
                        price = oi.Price
                    }).ToList()
                };

                return Json(new { success = true, order = orderData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading order details: " + ex.Message });
            }
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                if (Enum.TryParse<OrderStatus>(status, out var orderStatus))
                {
                    order.Status = orderStatus;
                    
                    if (orderStatus == OrderStatus.Delivered)
                    {
                        order.ActualDeliveryTime = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"Order {order.OrderNumber} status updated to {status}" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid status value" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating order status: " + ex.Message });
            }
        }

        // GET: /Admin/PointsRewards
        public async Task<IActionResult> PointsRewards()
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && successMessage.Contains("Order placed"))
            {
                TempData.Remove("SuccessMessage");
            }
            var rewards = await _context.PointsRewards
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rewards);
        }

        // GET: /Admin/PointsRewards/Create
        public IActionResult CreatePointsReward()
        {
            return View();
        }

        // POST: /Admin/PointsRewards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePointsReward(PointsReward reward)
        {
            if (ModelState.IsValid)
            {
                reward.CreatedAt = DateTime.UtcNow;
                _context.PointsRewards.Add(reward);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Points reward created successfully!";
                return RedirectToAction(nameof(PointsRewards));
            }
            return View(reward);
        }

        // GET: /Admin/PointsRewards/Edit/5
        public async Task<IActionResult> EditPointsReward(int id)
        {
            var reward = await _context.PointsRewards.FindAsync(id);
            if (reward == null)
            {
                return NotFound();
            }
            return View(reward);
        }

        // POST: /Admin/PointsRewards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPointsReward(int id, PointsReward reward)
        {
            if (id != reward.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    reward.UpdatedAt = DateTime.UtcNow;
                    _context.Update(reward);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Points reward updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PointsRewardExists(reward.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(PointsRewards));
            }
            return View(reward);
        }

        // POST: /Admin/PointsRewards/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePointsReward(int id)
        {
            var reward = await _context.PointsRewards.FindAsync(id);
            if (reward != null)
            {
                _context.PointsRewards.Remove(reward);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Points reward deleted successfully!";
            }
            return RedirectToAction(nameof(PointsRewards));
        }

        // GET: /Admin/RedemptionHistory
        public async Task<IActionResult> RedemptionHistory()
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && successMessage.Contains("Order placed"))
            {
                TempData.Remove("SuccessMessage");
            }
            var redemptions = await _context.UserRedemptions
                .Include(r => r.User)
                .Include(r => r.PointsReward)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();

            return View(redemptions);
        }

        // POST: /Admin/MarkRedemptionAsUsed
        [HttpPost]
        public async Task<IActionResult> MarkRedemptionAsUsed([FromBody] int id)
        {
            try
            {
                var redemption = await _context.UserRedemptions.FindAsync(id);
                if (redemption == null)
                {
                    return Json(new { success = false, message = "Redemption not found" });
                }

                if (redemption.IsUsed)
                {
                    return Json(new { success = false, message = "Redemption already marked as used" });
                }

                redemption.IsUsed = true;
                redemption.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Redemption marked as used successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating redemption: " + ex.Message });
            }
        }

        // POST: /Admin/UnblockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser([FromBody] UnblockUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsBlocked = false;
                user.BlockedUntil = null;
                user.BlockReason = null;
                user.LoginAttempts = 0;
                user.LastLoginAttempt = null;

                await _userManager.UpdateAsync(user);

                return Json(new { 
                    success = true, 
                    message = $"User {user.UserName} has been unblocked successfully" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error unblocking user: " + ex.Message });
            }
        }

        private bool PointsRewardExists(int id)
        {
            return _context.PointsRewards.Any(e => e.Id == id);
        }

        // GET: /Admin/OrderCancellations
        public async Task<IActionResult> OrderCancellations()
        {
            var cancellations = await _context.OrderCancellations
                .Include(c => c.Order)
                .Include(c => c.User)
                .OrderByDescending(c => c.CancelledAt)
                .ToListAsync();

            return View(cancellations);
        }

        // POST: /Admin/MarkCancellationAsReviewed
        [HttpPost]
        public async Task<IActionResult> MarkCancellationAsReviewed([FromBody] int id)
        {
            try
            {
                var cancellation = await _context.OrderCancellations.FindAsync(id);
                if (cancellation == null)
                {
                    return Json(new { success = false, message = "Cancellation not found" });
                }

                if (cancellation.IsReviewedByAdmin)
                {
                    return Json(new { success = false, message = "Cancellation already reviewed" });
                }

                cancellation.IsReviewedByAdmin = true;
                cancellation.ReviewedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cancellation marked as reviewed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating cancellation: " + ex.Message });
            }
        }

        // POST: /Admin/AddAdminNotes
        [HttpPost]
        public async Task<IActionResult> AddAdminNotes([FromBody] AdminNotesRequest request)
        {
            try
            {
                var cancellation = await _context.OrderCancellations.FindAsync(request.CancellationId);
                if (cancellation == null)
                {
                    return Json(new { success = false, message = "Cancellation not found" });
                }

                cancellation.AdminNotes = request.Notes;
                cancellation.IsReviewedByAdmin = true;
                cancellation.ReviewedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Admin notes added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding admin notes: " + ex.Message });
            }
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && successMessage.Contains("Order placed"))
            {
                TempData.Remove("SuccessMessage");
            }
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (ApplicationUser user in users)
            {
                var thisViewModel = new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePhotoUrl = user.ProfilePhotoUrl,
                    Points = user.Points,
                    Roles = await _userManager.GetRolesAsync(user),
                    IsBlocked = user.IsBlocked,
                    BlockedUntil = user.BlockedUntil,
                    BlockReason = user.BlockReason,
                    LoginAttempts = user.LoginAttempts,
                    LastLoginAttempt = user.LastLoginAttempt,
                    LastLoginDate = user.LastLoginDate
                };
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }

        // POST: /Admin/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string role, bool isInRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Prevent changing admin roles
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Json(new { success = false, message = "Cannot modify admin user roles" });
                }

                if (isInRole)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }

                return Json(new { success = true, message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user role: " + ex.Message });
            }
        }

        // POST: /Admin/BlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userId, string reason, int blockDurationHours = 24)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Prevent blocking admin users
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Json(new { success = false, message = "Cannot block admin users" });
                }

                var blockedUntil = DateTime.UtcNow.AddHours(blockDurationHours);
                await _loginSecurityService.BlockUserAsync(userId, reason, blockedUntil);

                return Json(new { 
                    success = true, 
                    message = $"User blocked successfully until {blockedUntil:yyyy-MM-dd HH:mm:ss} UTC" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error blocking user: " + ex.Message });
            }
        }

        // POST: /Admin/UnblockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                await _loginSecurityService.UnblockUserAsync(userId);

                return Json(new { success = true, message = "User unblocked successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error unblocking user: " + ex.Message });
            }
        }

        // POST: /Admin/ChangeUserPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPassword(string userId, string newPassword, string confirmPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Prevent changing admin passwords
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Json(new { success = false, message = "Cannot change admin user passwords" });
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return Json(new { success = false, message = "Password cannot be empty" });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match" });
                }

                // Check password complexity requirements
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var validationResult = await passwordValidator.ValidateAsync(_userManager, user, newPassword);
                
                if (!validationResult.Succeeded)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Password validation failed: {errors}" });
                }

                // Generate password reset token and change password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    // Log the password change
                    _logger.LogInformation("Admin changed password for user {UserId} ({UserName})", userId, user.UserName);
                    
                    return Json(new { success = true, message = "User password changed successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to change password: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return Json(new { success = false, message = "Error changing user password: " + ex.Message });
            }
        }

        // POST: /Admin/UpdateUserPoints
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserPoints(string userId, int points)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Users");
                }

                if (points < 0)
                {
                    TempData["ErrorMessage"] = "Points cannot be negative.";
                    return RedirectToAction("Users");
                }

                user.Points = points;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Points updated successfully for {user.UserName}. New points: {points}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update user points.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while updating user points.";
            }

            return RedirectToAction("Users");
        }

        // POST: /Admin/UpdateUserInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserInfo(string userId, string email, string phoneNumber)
        {
            try
            {
                _logger.LogInformation("UpdateUserInfo called with userId: {UserId}, email: {Email}, phoneNumber: {PhoneNumber}", 
                    userId, email, phoneNumber);
                
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return Json(new { success = false, message = "User not found" });
                }

                // Prevent editing admin users
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogWarning("Attempted to edit admin user: {UserId} ({UserName})", userId, user.UserName);
                    return Json(new { success = false, message = "Cannot edit admin user information" });
                }

                // Update email if provided and different
                if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
                {
                    user.Email = email;
                    user.NormalizedEmail = email.ToUpperInvariant();
                }

                // Update phone number
                user.PhoneNumber = phoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin updated user information for user {UserId} ({UserName})", userId, user.UserName);
                    return Json(new { success = true, message = "User information updated successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to update user information: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user information for user {UserId}", userId);
                return Json(new { success = false, message = "Error updating user information: " + ex.Message });
            }
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            var oldStatus = order.Status;
            order.Status = newStatus;

            // Update timestamps based on status
            switch (newStatus)
            {
                case OrderStatus.Confirmed:
                    // Order confirmed timestamp
                    break;
                case OrderStatus.Preparing:
                    // Order preparing timestamp
                    break;
                case OrderStatus.Ready:
                    // Order ready timestamp
                    break;
                case OrderStatus.OutForDelivery:
                    // Out for delivery timestamp
                    break;
                case OrderStatus.Delivered:
                    order.ActualDeliveryTime = DateTime.UtcNow;
                    break;
                case OrderStatus.Cancelled:
                    order.CancelledAt = DateTime.UtcNow;
                    order.CancelledBy = "Admin";
                    break;
            }

            await _context.SaveChangesAsync();

            // Send SMS notification to customer
            try
            {
                if (order.User != null && !string.IsNullOrEmpty(order.User.PhoneNumber))
                {
                    var smsService = HttpContext.RequestServices.GetRequiredService<SmsService>();
                    var statusMessage = GetOrderStatusMessage(newStatus, order.OrderNumber);
                    await smsService.SendSmsAsync(order.User.PhoneNumber, statusMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notification for order {OrderId}", orderId);
            }

            TempData["SuccessMessage"] = $"Order status updated from {oldStatus} to {newStatus}.";
            return Json(new { success = true, message = "Order status updated successfully." });
        }

        private string GetOrderStatusMessage(OrderStatus status, string orderNumber)
        {
            return status switch
            {
                OrderStatus.Confirmed => $"MackDihh: Your order #{orderNumber} has been confirmed and is being prepared!",
                OrderStatus.Preparing => $"MackDihh: Your order #{orderNumber} is being prepared. It will be ready soon!",
                OrderStatus.Ready => $"MackDihh: Your order #{orderNumber} is ready for pickup/delivery!",
                OrderStatus.OutForDelivery => $"MackDihh: Your order #{orderNumber} is out for delivery!",
                OrderStatus.Delivered => $"MackDihh: Your order #{orderNumber} has been delivered. Thank you!",
                OrderStatus.Cancelled => $"MackDihh: Your order #{orderNumber} has been cancelled. Please contact support if you have questions.",
                _ => $"MackDihh: Your order #{orderNumber} status has been updated."
            };
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            try
            {
                // Get basic order counts first
                var totalOrders = await _context.Orders.CountAsync();
                var totalSales = await _context.Orders.SumAsync(o => o.Total);
                
                // Calculate average order value safely
                var averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;
                
                // Get recent performance data (Last 30 days)
                var recentOrdersQuery = _context.Orders.Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30));
                var recentSales = await recentOrdersQuery.SumAsync(o => o.Total);
                var recentOrders = await recentOrdersQuery.CountAsync();
                
                // Get top selling items with null safety
                var topSellingItems = await _context.OrderItems
                    .Include(oi => oi.MenuItem)
                    .Where(oi => oi.MenuItem != null) // Ensure MenuItem exists
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g => new TopSellingItem
                    {
                        MenuItemName = g.First().MenuItem.Name ?? "Unknown Item",
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToListAsync();
                
                // Get order status distribution
                var orderStatusDistribution = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new OrderStatusCount
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();
                
                // Get customer analytics
                var totalCustomers = await _context.Users.CountAsync();
                var activeCustomers = await _context.Users
                    .Where(u => u.LastLoginDate.HasValue && u.LastLoginDate >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();
                
                // Get monthly revenue data (Last 12 months)
                var monthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new MonthlyRevenue
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(o => o.Total),
                        OrderCount = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var reportsViewModel = new ReportsViewModel
                {
                    // Sales Analytics
                    TotalSales = totalSales,
                    TotalOrders = totalOrders,
                    AverageOrderValue = averageOrderValue,
                    
                    // Recent Performance (Last 30 days)
                    RecentSales = recentSales,
                    RecentOrders = recentOrders,
                    
                    // Top Selling Items
                    TopSellingItems = topSellingItems,
                    
                    // Order Status Distribution
                    OrderStatusDistribution = orderStatusDistribution,
                    
                    // Customer Analytics
                    TotalCustomers = totalCustomers,
                    ActiveCustomers = activeCustomers,
                    
                    // Revenue by Month (Last 12 months)
                    MonthlyRevenue = monthlyRevenue
                };

                return View(reportsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports data");
                TempData["ErrorMessage"] = "An error occurred while loading reports. Please try again.";
                
                // Return empty view model to prevent crashes
                return View(new ReportsViewModel());
            }
        }

        // GET: /Admin/PromoCodes
        public async Task<IActionResult> PromoCodes()
        {
            // Clear any non-admin related success messages
            var successMessage = TempData["SuccessMessage"]?.ToString();
            if (successMessage != null && (successMessage.Contains("Order placed") || successMessage.Contains("points")))
            {
                TempData.Remove("SuccessMessage");
            }
            
            var promoCodes = await _context.Deals
                .Where(d => !string.IsNullOrEmpty(d.PromoCode))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(promoCodes);
        }

        // GET: /Admin/PromoCodes/Create
        public IActionResult CreatePromoCode()
        {
            return View();
        }

        // POST: /Admin/PromoCodes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromoCode(Deal deal)
        {
            _logger.LogInformation($"CreatePromoCode called with Title: {deal.Title}, PromoCode: {deal.PromoCode}");
            
            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                _logger.LogError("ModelState is not valid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError($"ModelState Error: {error.ErrorMessage}");
                }
                TempData["ErrorMessage"] = "Please fix the validation errors and try again.";
                return View(deal);
            }

            try
            {
                _logger.LogInformation("ModelState is valid, proceeding with creation");
                
                // Check if promo code already exists
                var existingDeal = await _context.Deals
                    .FirstOrDefaultAsync(d => d.PromoCode == deal.PromoCode);
                
                if (existingDeal != null)
                {
                    _logger.LogWarning($"Promo code {deal.PromoCode} already exists");
                    ModelState.AddModelError("PromoCode", "This promo code already exists.");
                    TempData["ErrorMessage"] = "This promo code already exists. Please choose a different code.";
                    return View(deal);
                }

                deal.CreatedAt = DateTime.UtcNow;
                deal.IsActive = true;
                
                // Handle "Never Expires" case - if start date is 0001 and end date is 2099, set EndDate to null
                if (deal.StartDate.Year == 1 && deal.EndDate.HasValue && deal.EndDate.Value.Year == 2099)
                {
                    deal.EndDate = null;
                    // Set start date to current time for never-expiring promos
                    deal.StartDate = DateTime.UtcNow;
                }
                
                _logger.LogInformation($"Adding deal to context: {deal.Title}");
                _context.Deals.Add(deal);
                
                _logger.LogInformation("Saving changes to database");
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Promo code created successfully");
                TempData["SuccessMessage"] = "Promo code created successfully!";
                return RedirectToAction(nameof(PromoCodes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promo code");
                TempData["ErrorMessage"] = "An error occurred while creating the promo code. Please try again.";
                return View(deal);
            }
        }

        // GET: /Admin/PromoCodes/Edit/{id}
        public async Task<IActionResult> EditPromoCode(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null)
            {
                return NotFound();
            }
            
            return View(deal);
        }

        // POST: /Admin/PromoCodes/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromoCode(int id, Deal deal)
        {
            if (id != deal.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if promo code already exists (excluding current deal)
                    var existingDeal = await _context.Deals
                        .FirstOrDefaultAsync(d => d.PromoCode == deal.PromoCode && d.Id != id);
                    
                    if (existingDeal != null)
                    {
                        ModelState.AddModelError("PromoCode", "This promo code already exists.");
                        return View(deal);
                    }

                    deal.UpdatedAt = DateTime.UtcNow;
                    
                    // Handle "Never Expires" case - if start date is 0001 and end date is 2099, set EndDate to null
                    if (deal.StartDate.Year == 1 && deal.EndDate.HasValue && deal.EndDate.Value.Year == 2099)
                    {
                        deal.EndDate = null;
                        // Set start date to current time for never-expiring promos
                        deal.StartDate = DateTime.UtcNow;
                    }
                    
                    _context.Update(deal);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Promo code updated successfully!";
                    return RedirectToAction(nameof(PromoCodes));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealExists(deal.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            return View(deal);
        }

        // POST: /Admin/PromoCodes/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromoCode(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal != null)
            {
                _context.Deals.Remove(deal);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Promo code deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Promo code not found.";
            }
            
            return RedirectToAction(nameof(PromoCodes));
        }

        // POST: /Admin/PromoCodes/ToggleStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromoCodeStatus(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal != null)
            {
                deal.IsActive = !deal.IsActive;
                deal.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                var status = deal.IsActive ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"Promo code {status} successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Promo code not found.";
            }
            
            return RedirectToAction(nameof(PromoCodes));
        }

        private bool DealExists(int id)
        {
            return _context.Deals.Any(e => e.Id == id);
        }

        // GET: /Admin/AutoResponses
        public async Task<IActionResult> AutoResponses()
        {
            var autoResponses = await _context.AutoResponses
                .Include(ar => ar.Creator)
                .Include(ar => ar.Updater)
                .OrderByDescending(ar => ar.CreatedAt)
                .ToListAsync();

            return View(autoResponses);
        }

        // GET: /Admin/AutoResponses/Create
        public IActionResult CreateAutoResponse()
        {
            return View();
        }

        // POST: /Admin/AutoResponses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAutoResponse(AutoResponse autoResponse)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                autoResponse.CreatedBy = userId;
                autoResponse.CreatedAt = DateTime.UtcNow;

                _context.AutoResponses.Add(autoResponse);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Auto-response created successfully!";
                return RedirectToAction("AutoResponses");
            }

            return View(autoResponse);
        }

        // GET: /Admin/AutoResponses/Edit/{id}
        public async Task<IActionResult> EditAutoResponse(int id)
        {
            var autoResponse = await _context.AutoResponses.FindAsync(id);
            if (autoResponse == null)
            {
                return NotFound();
            }

            return View(autoResponse);
        }

        // POST: /Admin/AutoResponses/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAutoResponse(int id, AutoResponse autoResponse)
        {
            if (id != autoResponse.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    autoResponse.UpdatedBy = userId;
                    autoResponse.UpdatedAt = DateTime.UtcNow;

                    _context.Update(autoResponse);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Auto-response updated successfully!";
                    return RedirectToAction("AutoResponses");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AutoResponseExists(autoResponse.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(autoResponse);
        }

        // POST: /Admin/AutoResponses/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoResponse(int id)
        {
            var autoResponse = await _context.AutoResponses.FindAsync(id);
            if (autoResponse != null)
            {
                _context.AutoResponses.Remove(autoResponse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Auto-response deleted successfully!";
            }

            return RedirectToAction("AutoResponses");
        }

        // POST: /Admin/AutoResponses/ToggleStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAutoResponseStatus(int id)
        {
            var autoResponse = await _context.AutoResponses.FindAsync(id);
            if (autoResponse != null)
            {
                autoResponse.IsActive = !autoResponse.IsActive;
                autoResponse.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                autoResponse.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Json(new { success = true, isActive = autoResponse.IsActive });
            }

            return Json(new { success = false, message = "Auto-response not found" });
        }

        private bool AutoResponseExists(int id)
        {
            return _context.AutoResponses.Any(e => e.Id == id);
        }

    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public int PendingOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<TopSellingItem> TopSellingItems { get; set; } = new();
    }

    public class TopSellingItem
    {
        public string MenuItemName { get; set; } = "";
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AdminNotesRequest
    {
        public int CancellationId { get; set; }
        public string Notes { get; set; } = "";
    }

    public class UnblockUserRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class ReportsViewModel
    {
        // Sales Analytics
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Recent Performance
        public decimal RecentSales { get; set; }
        public int RecentOrders { get; set; }
        
        // Top Selling Items
        public List<TopSellingItem> TopSellingItems { get; set; } = new();
        
        // Order Status Distribution
        public List<OrderStatusCount> OrderStatusDistribution { get; set; } = new();
        
        // Customer Analytics
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        
        // Monthly Revenue
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new();
    }

    public class OrderStatusCount
    {
        public OrderStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }
}
