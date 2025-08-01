using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    [Authorize] // This ensures only logged-in users can access this page
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
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

            return View(orders);
        }
    }
}
