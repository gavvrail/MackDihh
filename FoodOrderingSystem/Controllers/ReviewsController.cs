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

            // Get current user's votes for these reviews
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Dictionary<int, VoteType?> userVotes = new();
            
            if (!string.IsNullOrEmpty(userId))
            {
                var reviewIds = reviews.Select(r => r.Id).ToList();
                var votes = await _context.ReviewVotes
                    .Where(rv => rv.UserId == userId && reviewIds.Contains(rv.ReviewId))
                    .ToListAsync();
                
                userVotes = votes.ToDictionary(v => v.ReviewId, v => (VoteType?)v.VoteType);
            }

            var viewModel = new ProductReviewsViewModel
            {
                MenuItem = menuItem,
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalReviews = totalReviews,
                PageSize = pageSize,
                UserVotes = userVotes,
                CurrentUserId = userId
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

            // Allow all registered users to write reviews
            // Removed the purchase requirement to make reviews more accessible

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
        public async Task<IActionResult> Create(int menuItemId, CreateReviewViewModel model)
        {
            _logger.LogInformation("POST Create called with MenuItemId: {MenuItemId}, Model Rating: {Rating}", menuItemId, model.Rating);
            
            // Ensure the model has the correct MenuItemId
            model.MenuItemId = menuItemId;
            
            // Log model state for debugging
            _logger.LogInformation("Model state - Valid: {IsValid}, Rating: {Rating}, Comment: {Comment}, IsAnonymous: {IsAnonymous}", 
                ModelState.IsValid, model.Rating, model.Comment, model.IsAnonymous);
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                
                // Reload the menu item for the view
                model.MenuItem = await _context.MenuItems
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.Id == menuItemId) ?? new MenuItem();
                
                TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Validate menu item exists and is available
            var menuItem = await _context.MenuItems
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

            // Allow all registered users to write reviews
            // Removed the purchase requirement to make reviews more accessible

            try
            {
                var review = new Review
                {
                    UserId = userId,
                    MenuItemId = menuItemId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    IsAnonymous = model.IsAnonymous,
                    AnonymousName = model.IsAnonymous ? model.AnonymousName : null,
                    CreatedDate = DateTime.UtcNow,
                    IsVerified = true // Auto-approve reviews for better user experience
                };

                _logger.LogInformation("Creating review: UserId={UserId}, MenuItemId={MenuItemId}, Rating={Rating}, IsVerified={IsVerified}", 
                    userId, menuItemId, model.Rating, review.IsVerified);

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Review saved successfully with ID: {ReviewId}", review.Id);

                // Update menu item average rating
                await UpdateMenuItemRating(menuItemId);

                _logger.LogInformation("Menu item rating updated for MenuItemId: {MenuItemId}", menuItemId);

                TempData["SuccessMessage"] = "Your review has been submitted and is now visible!";
                return RedirectToAction("Product", new { menuItemId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for MenuItemId: {MenuItemId}, UserId: {UserId}", menuItemId, userId);
                
                // Reload the menu item for the view
                model.MenuItem = await _context.MenuItems
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.Id == menuItemId) ?? new MenuItem();
                
                TempData["ErrorMessage"] = "There was an error submitting your review. Please try again.";
                return View(model);
            }
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

            // Check if user has already voted on this review
            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(rv => rv.ReviewId == reviewId && rv.UserId == userId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == VoteType.Helpful)
                {
                    // User is un-voting their helpful vote
                    _context.ReviewVotes.Remove(existingVote);
                    review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { 
                        success = true, 
                        helpfulCount = review.HelpfulCount,
                        unhelpfulCount = review.UnhelpfulCount,
                        userVote = "none"
                    });
                }
                else
                {
                    // User is changing from unhelpful to helpful
                    existingVote.VoteType = VoteType.Helpful;
                    existingVote.CreatedDate = DateTime.UtcNow;
                    review.UnhelpfulCount = Math.Max(0, review.UnhelpfulCount - 1);
                    review.HelpfulCount++;
                }
            }
            else
            {
                // User is voting helpful for the first time
                var newVote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    VoteType = VoteType.Helpful,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ReviewVotes.Add(newVote);
                review.HelpfulCount++;
            }

            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                helpfulCount = review.HelpfulCount,
                unhelpfulCount = review.UnhelpfulCount,
                userVote = "helpful"
            });
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

            // Check if user has already voted on this review
            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(rv => rv.ReviewId == reviewId && rv.UserId == userId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == VoteType.Unhelpful)
                {
                    // User is un-voting their unhelpful vote
                    _context.ReviewVotes.Remove(existingVote);
                    review.UnhelpfulCount = Math.Max(0, review.UnhelpfulCount - 1);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { 
                        success = true, 
                        helpfulCount = review.HelpfulCount,
                        unhelpfulCount = review.UnhelpfulCount,
                        userVote = "none"
                    });
                }
                else
                {
                    // User is changing from helpful to unhelpful
                    existingVote.VoteType = VoteType.Unhelpful;
                    existingVote.CreatedDate = DateTime.UtcNow;
                    review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                    review.UnhelpfulCount++;
                }
            }
            else
            {
                // User is voting unhelpful for the first time
                var newVote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    VoteType = VoteType.Unhelpful,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ReviewVotes.Add(newVote);
                review.UnhelpfulCount++;
            }

            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                helpfulCount = review.HelpfulCount,
                unhelpfulCount = review.UnhelpfulCount,
                userVote = "unhelpful"
            });
        }

        // POST: /Reviews/DeleteMyReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMyReview(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found or you don't have permission to delete it" });
            }

            var menuItemId = review.MenuItemId;
            
            // Delete associated votes first
            var votes = await _context.ReviewVotes
                .Where(rv => rv.ReviewId == reviewId)
                .ToListAsync();
            
            if (votes.Any())
            {
                _context.ReviewVotes.RemoveRange(votes);
            }
            
            // Delete the review
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Update menu item average rating
            await UpdateMenuItemRating(menuItemId);

            _logger.LogInformation("User {UserId} deleted their own review {ReviewId}", userId, reviewId);

            return Json(new { success = true, message = "Review deleted successfully" });
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
        public Dictionary<int, VoteType?> UserVotes { get; set; } = new();
        public string? CurrentUserId { get; set; }
    }

    public class CreateReviewViewModel : IValidatableObject
    {
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review cannot be longer than 1000 characters")]
        public string? Comment { get; set; }

        public bool IsAnonymous { get; set; }

        [StringLength(200)]
        public string? AnonymousName { get; set; }

        // Custom validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsAnonymous && string.IsNullOrWhiteSpace(AnonymousName))
            {
                yield return new ValidationResult("Anonymous name is required when posting anonymously.", new[] { nameof(AnonymousName) });
            }
        }
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
