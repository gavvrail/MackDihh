using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
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

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin (Dashboard)
        public async Task<IActionResult> Index()
        {
            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalSales = await _context.Orders.SumAsync(o => o.Total),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
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
        public async Task<IActionResult> Orders(string status = "", string search = "")
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
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
            return View(orders);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = status;
                if (status == OrderStatus.Delivered)
                {
                    order.ActualDeliveryTime = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Order {order.OrderNumber} status updated to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating order status: " + ex.Message });
            }
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (ApplicationUser user in users)
            {
                var thisViewModel = new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = await _userManager.GetRolesAsync(user)
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

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            var reportViewModel = new ReportViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalSales = await _context.Orders.SumAsync(o => o.Total),
                MonthlySales = await GetMonthlySales(),
                OrderStatusBreakdown = await GetOrderStatusBreakdown(),
                TopCategories = await GetTopCategories()
            };

            return View(reportViewModel);
        }

        private async Task<List<ViewModels.MonthlySalesData>> GetMonthlySales()
        {
            var currentYear = DateTime.UtcNow.Year;
            var monthlySales = await _context.Orders
                .Where(o => o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new ViewModels.MonthlySalesData
                {
                    Month = g.Key,
                    Sales = g.Sum(o => o.Total),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            return monthlySales;
        }

        private async Task<List<ViewModels.OrderStatusData>> GetOrderStatusBreakdown()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new ViewModels.OrderStatusData
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (decimal)g.Count() / _context.Orders.Count() * 100
                })
                .ToListAsync();
        }

        private async Task<List<ViewModels.CategorySalesData>> GetTopCategories()
        {
            return await _context.OrderItems
                .Include(oi => oi.MenuItem)
                .ThenInclude(mi => mi.Category)
                .GroupBy(oi => oi.MenuItem.Category!.Name)
                .Select(g => new ViewModels.CategorySalesData
                {
                    CategoryName = g.Key,
                    TotalSales = g.Sum(oi => oi.Quantity * oi.Price),
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();
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
}
