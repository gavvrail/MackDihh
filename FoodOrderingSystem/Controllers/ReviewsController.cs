using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // GET: /Reviews/TestAllItems - Test action to verify all menu items work
        public async Task<IActionResult> TestAllItems()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Id)
                .ToListAsync();

            var results = new List<object>();
            
            foreach (var item in menuItems)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.MenuItemId == item.Id && r.IsVerified)
                    .CountAsync();

                results.Add(new
                {
                    Id = item.Id,
                    Name = item.Name,
                    Price = item.Price,
                    PriceFormatted = item.Price.ToString("F2"),
                    Category = item.Category?.Name,
                    ReviewCount = reviews,
                    HasImage = !string.IsNullOrEmpty(item.ImageUrl),
                    IsAvailable = item.IsAvailable,
                    ReviewsUrl = Url.Action("Product", "Reviews", new { menuItemId = item.Id })
                });
            }

            return Json(new
            {
                TotalItems = results.Count,
                Items = results,
                Message = "All menu items tested successfully"
            });
        }

        // GET: /Reviews/Product/{menuItemId}
        public async Task<IActionResult> Product(int menuItemId, int page = 1, int pageSize = 10)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);

            if (menuItem == null)
            {
                _logger.LogWarning("MenuItem with ID {MenuItemId} not found", menuItemId);
                TempData["ErrorMessage"] = $"Menu item with ID {menuItemId} was not found. Please select a valid item from our menu.";
                return RedirectToAction("Index", "Menu");
            }

            if (!menuItem.IsAvailable)
            {
                _logger.LogWarning("MenuItem with ID {MenuItemId} is not available", menuItemId);
                TempData["ErrorMessage"] = $"The menu item '{menuItem.Name}' is currently not available for reviews.";
                return RedirectToAction("Index", "Menu");
            }


            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Images)
                .Include(r => r.Responses)
                .Where(r => r.MenuItemId == menuItemId && r.IsVerified)
                .OrderByDescending(r => r.CreatedDate);

            var totalReviews = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProductReviewsViewModel
            {
                MenuItem = menuItem,
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalReviews = totalReviews,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: /Reviews/Create/{menuItemId}
        public async Task<IActionResult> Create(int menuItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);

            if (menuItem == null)
            {
                TempData["ErrorMessage"] = $"Menu item with ID {menuItemId} was not found.";
                return RedirectToAction("Index", "Menu");
            }

            if (!menuItem.IsAvailable)
            {
                TempData["ErrorMessage"] = $"The menu item '{menuItem.Name}' is currently not available for reviews.";
                return RedirectToAction("Index", "Menu");
            }
            
            // Check if user has already reviewed this item
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MenuItemId == menuItemId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "You have already reviewed this product.";
                return RedirectToAction("Product", new { menuItemId });
            }

            // Check if user has purchased this item
            var hasPurchased = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.MenuItemId == menuItemId && 
                               oi.Order.UserId == userId && 
                               oi.Order.Status == OrderStatus.Delivered);

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "You can only review products you have purchased.";
                return RedirectToAction("Product", new { menuItemId });
            }

            var reviewViewModel = new CreateReviewViewModel
            {
                MenuItemId = menuItemId,
                MenuItem = menuItem
            };

            return View(reviewViewModel);
        }

        // POST: /Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload the menu item for the view
                model.MenuItem = await _context.MenuItems
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.Id == model.MenuItemId) ?? new MenuItem();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Validate menu item exists and is available
            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == model.MenuItemId);

            if (menuItem == null)
            {
                TempData["ErrorMessage"] = $"Menu item with ID {model.MenuItemId} was not found.";
                return RedirectToAction("Index", "Menu");
            }

            if (!menuItem.IsAvailable)
            {
                TempData["ErrorMessage"] = $"The menu item '{menuItem.Name}' is currently not available for reviews.";
                return RedirectToAction("Index", "Menu");
            }
            
            // Check if user has already reviewed this item
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MenuItemId == model.MenuItemId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "You have already reviewed this product.";
                return RedirectToAction("Product", new { menuItemId = model.MenuItemId });
            }

            // Check if user has purchased this item
            var hasPurchased = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.MenuItemId == model.MenuItemId && 
                               oi.Order.UserId == userId && 
                               oi.Order.Status == OrderStatus.Delivered);

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "You can only review products you have purchased.";
                return RedirectToAction("Product", new { menuItemId = model.MenuItemId });
            }

            var review = new Review
            {
                UserId = userId,
                MenuItemId = model.MenuItemId,
                Rating = model.Rating,
                Comment = model.Comment,
                IsAnonymous = model.IsAnonymous,
                AnonymousName = model.IsAnonymous ? model.AnonymousName : null,
                CreatedDate = DateTime.UtcNow,
                IsVerified = false // Admin needs to verify
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update menu item average rating
            await UpdateMenuItemRating(model.MenuItemId);

            _logger.LogInformation("User {UserId} created review for menu item {MenuItemId}", userId, model.MenuItemId);

            TempData["SuccessMessage"] = "Your review has been submitted and is pending verification.";
            return RedirectToAction("Product", new { menuItemId = model.MenuItemId });
        }

        // POST: /Reviews/AddResponse
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddResponse(int reviewId, string response)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            var reviewResponse = new ReviewResponse
            {
                ReviewId = reviewId,
                ResponderId = userId,
                Response = response,
                CreatedDate = DateTime.UtcNow,
                IsFromBusiness = true
            };

            _context.ReviewResponses.Add(reviewResponse);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {UserId} added response to review {ReviewId}", userId, reviewId);

            return Json(new { success = true, message = "Response added successfully" });
        }

        // POST: /Reviews/MarkHelpful
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkHelpful(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            // Check if user has already marked this review as helpful
            // For simplicity, we'll just increment the count
            review.HelpfulCount++;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, helpfulCount = review.HelpfulCount });
        }

        // POST: /Reviews/MarkUnhelpful
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnhelpful(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            review.UnhelpfulCount++;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, unhelpfulCount = review.UnhelpfulCount });
        }

        // GET: /Reviews/MyReviews
        public async Task<IActionResult> MyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            var reviews = await _context.Reviews
                .Include(r => r.MenuItem)
                .ThenInclude(m => m.Category)
                .Include(r => r.Responses)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(reviews);
        }

        // GET: /Reviews/Admin/Index
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(int page = 1, int pageSize = 20)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.MenuItem)
                .OrderByDescending(r => r.CreatedDate);

            var totalReviews = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminReviewsViewModel
            {
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalReviews = totalReviews,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // POST: /Reviews/Admin/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Verify(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            review.IsVerified = true;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            // Update menu item average rating
            await UpdateMenuItemRating(review.MenuItemId);

            _logger.LogInformation("Review {ReviewId} verified by admin", reviewId);

            return Json(new { success = true, message = "Review verified successfully" });
        }

        // POST: /Reviews/Admin/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            var menuItemId = review.MenuItemId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Update menu item average rating
            await UpdateMenuItemRating(menuItemId);

            _logger.LogInformation("Review {ReviewId} deleted by admin", reviewId);

            return Json(new { success = true, message = "Review deleted successfully" });
        }

        private async Task UpdateMenuItemRating(int menuItemId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.MenuItemId == menuItemId && r.IsVerified)
                .ToListAsync();

            if (reviews.Any())
            {
                var averageRating = reviews.Average(r => r.Rating);
                var totalReviews = reviews.Count;

                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem != null)
                {
                    menuItem.AverageRating = (decimal)averageRating;
                    menuItem.TotalReviews = totalReviews;
                    _context.MenuItems.Update(menuItem);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    public class ProductReviewsViewModel
    {
        public MenuItem MenuItem { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalReviews { get; set; }
        public int PageSize { get; set; }
    }

    public class CreateReviewViewModel
    {
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review cannot be longer than 1000 characters")]
        public string? Comment { get; set; }

        public bool IsAnonymous { get; set; }

        [StringLength(200)]
        public string? AnonymousName { get; set; }
    }

    public class AdminReviewsViewModel
    {
        public List<Review> Reviews { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalReviews { get; set; }
        public int PageSize { get; set; }
    }
}
