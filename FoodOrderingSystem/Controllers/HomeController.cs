using System.Diagnostics;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FoodOrderingSystem.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            
            // Get featured deals for home page
            var featuredDeals = await _context.Deals
                .Where(d => d.IsActive && d.StartDate <= DateTime.UtcNow && d.EndDate >= DateTime.UtcNow)
                .OrderBy(d => d.CreatedAt)
                .Take(4)
                .ToListAsync();

            var viewModel = new HomePageViewModel
            {
                FeaturedDeals = featuredDeals,
                User = user,
                UserPoints = user?.Points ?? 0,
                IsMember = user?.IsMember ?? false,
                IsStudentVerified = user?.IsStudentVerified ?? false
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class HomePageViewModel
    {
        public List<Deal> FeaturedDeals { get; set; } = new();
        public ApplicationUser? User { get; set; }
        public int UserPoints { get; set; }
        public bool IsMember { get; set; }
        public bool IsStudentVerified { get; set; }
    }
}
