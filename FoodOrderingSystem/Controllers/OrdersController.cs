﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Services;

namespace FoodOrderingSystem.Controllers
{
    [Authorize] // This ensures only logged-in users can access this page
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeZoneService _timeZoneService;

        public OrdersController(ApplicationDbContext context, TimeZoneService timeZoneService)
        {
            _context = context;
            _timeZoneService = timeZoneService;
        }

        // GET: /Orders/History
        public async Task<IActionResult> History()
        {
            // Get the ID of the currently logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find all orders that belong to this user
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems) // Include the list of items in each order
                .ThenInclude(oi => oi.MenuItem) // For each item, include the product details
                .OrderByDescending(o => o.OrderDate) // Show the most recent orders first
                .ToListAsync();

            // Update estimated delivery times for recent orders (within last 30 minutes)
            foreach (var order in orders)
            {
                if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled)
                {
                    var currentLocalTime = _timeZoneService.GetLocalTime();
                    var orderLocalTime = _timeZoneService.ConvertFromUtc(order.OrderDate);
                    var timeSinceOrder = currentLocalTime - orderLocalTime;
                    
                    // Only update if the order was placed within the last 30 minutes
                    if (timeSinceOrder.TotalMinutes <= 30)
                    {
                        order.EstimatedDeliveryTime = _timeZoneService.ConvertToUtc(_timeZoneService.GetLocalTimePlusMinutes(30));
                    }
                }
            }

            return View(orders);
        }

        // GET: /Orders/Track/{orderId}
        public async Task<IActionResult> Track(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Only update estimated delivery time for recent orders (within last 30 minutes)
            // This prevents old orders from showing unrealistic delivery times
            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled)
            {
                var currentLocalTime = _timeZoneService.GetLocalTime();
                var orderLocalTime = _timeZoneService.ConvertFromUtc(order.OrderDate);
                var timeSinceOrder = currentLocalTime - orderLocalTime;
                
                // Only update if the order was placed within the last 30 minutes
                if (timeSinceOrder.TotalMinutes <= 30)
                {
                    order.EstimatedDeliveryTime = _timeZoneService.ConvertToUtc(_timeZoneService.GetLocalTimePlusMinutes(30));
                    
                    // Update the database with the new estimated delivery time
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }
            }

            return View(order);
        }

        // GET: /Orders/Cancel/{id}
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Check if order can be cancelled (only pending or confirmed orders)
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "This order cannot be cancelled as it has already been processed.";
                return RedirectToAction(nameof(History));
            }

            var viewModel = new OrderCancellationViewModel
            {
                Order = order,
                CancellationReasons = GetCancellationReasons()
            };

            return View(viewModel);
        }

        // POST: /Orders/Cancel/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, OrderCancellationViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found or you don't have permission to cancel this order.";
                    return RedirectToAction(nameof(History));
                }

                if (!ModelState.IsValid)
                {
                    model.Order = order;
                    model.CancellationReasons = GetCancellationReasons();
                    
                    // Add error message for debugging
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["ErrorMessage"] = $"Form validation failed: {string.Join(", ", errors)}";
                    
                    return View(model);
                }

            // Check if order can be cancelled
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "This order cannot be cancelled as it has already been processed.";
                return RedirectToAction(nameof(History));
            }

            // Update order status
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledBy = "Customer";

            // Create cancellation record
            var cancellation = new OrderCancellation
            {
                OrderId = order.Id,
                UserId = userId ?? string.Empty,
                ReasonType = model.SelectedReason,
                AdditionalDetails = model.AdditionalDetails,
                CancelledAt = DateTime.UtcNow
            };

            // Refund points if any were used
            if (order.PointsUsed > 0)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Points += order.PointsUsed;
                    user.TotalPointsRedeemed -= order.PointsUsed;
                }
            }

                _context.OrderCancellations.Add(cancellation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Order cancelled successfully. We're sorry to see you go!";
                return RedirectToAction(nameof(History));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while cancelling the order: {ex.Message}";
                return RedirectToAction(nameof(History));
            }
        }

        private List<CancellationReasonType> GetCancellationReasons()
        {
            return Enum.GetValues<CancellationReasonType>().ToList();
        }
    }

    public class OrderCancellationViewModel
    {
        public Order Order { get; set; } = null!;
        
        [Required(ErrorMessage = "Please select a reason for cancellation")]
        public CancellationReasonType SelectedReason { get; set; }
        
        [StringLength(500, ErrorMessage = "Additional details cannot exceed 500 characters")]
        public string? AdditionalDetails { get; set; }
        
        public List<CancellationReasonType> CancellationReasons { get; set; } = new();
    }
}
