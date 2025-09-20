using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Security.Claims;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Controllers
{
    public class DealsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DealsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = !string.IsNullOrEmpty(userId) ? await _context.Users.FindAsync(userId) : null;
                
                var now = DateTime.UtcNow;
            var deals = await _context.Deals
                .Where(d => d.IsActive && d.StartDate <= now && (d.EndDate == null || d.EndDate >= now))
                .OrderBy(d => d.Type)
                .ThenBy(d => d.StartDate)
                .ToListAsync();

            // Get redeemable menu items for points redemption
            var redeemableItems = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category != null ? m.Category.Name : "")
                .ThenBy(m => m.Name)
                .ToListAsync();

            // Get user's available promo codes with usage information
            var availablePromoCodes = new List<PromoCodeWithUsage>();
            var promoCodeDeals = deals.Where(d => d.Type == DealType.PromoCode).ToList();
            
            if (user != null && promoCodeDeals.Any())
            {
                // Get all user promo code usage in one query to avoid N+1 problem
                var dealIds = promoCodeDeals.Select(d => d.Id).ToList();
                var userUsages = await _context.UserPromoCodes
                    .Where(upc => upc.UserId == userId && dealIds.Contains(upc.DealId))
                    .GroupBy(upc => upc.DealId)
                    .Select(g => new { DealId = g.Key, TotalUses = g.Sum(x => x.CurrentUses) })
                    .ToDictionaryAsync(x => x.DealId, x => x.TotalUses);
                
                foreach (var deal in promoCodeDeals)
                {
                    var userPromoCodeUsage = userUsages.GetValueOrDefault(deal.Id, 0);

                    // Include deal if:
                    // 1. It has unlimited uses (MaxUses = -1), OR
                    // 2. User hasn't reached the usage limit
                    if (deal.MaxUses == -1 || userPromoCodeUsage < deal.MaxUses)
                    {
                        availablePromoCodes.Add(new PromoCodeWithUsage
                        {
                            Deal = deal,
                            CurrentUses = userPromoCodeUsage,
                            MaxUses = deal.MaxUses
                        });
                    }
                }
            }
            else
            {
                // For non-logged in users, show all promo codes without usage info
                foreach (var deal in promoCodeDeals)
                {
                    availablePromoCodes.Add(new PromoCodeWithUsage
                    {
                        Deal = deal,
                        CurrentUses = 0,
                        MaxUses = deal.MaxUses
                    });
                }
            }

            var viewModel = new DealsViewModel
            {
                CurrentDeals = deals.Where(d => d.Type == DealType.LimitedTimeOffer).ToList(),
                FlashSales = deals.Where(d => d.IsFlashSale).ToList(),
                BundleOffers = deals.Where(d => d.Type == DealType.BundleOffer).ToList(),
                SeasonalDiscounts = deals.Where(d => d.IsSeasonal).ToList(),
                PromoCodesWithUsage = availablePromoCodes, // Show available promo codes with usage info
                User = user,
                UserPoints = user?.Points ?? 0,
                RedeemableItems = redeemableItems
            };

                return View(viewModel);
            }
            catch (Exception)
            {
                // Log the error (you can use ILogger here)
                TempData["ErrorMessage"] = "An error occurred while loading deals. Please try again later.";
                
                // Return a basic view model to prevent crashes
                return View(new DealsViewModel
                {
                    User = null,
                    UserPoints = 0,
                    RedeemableItems = new List<MenuItem>(),
                    PromoCodesWithUsage = new List<PromoCodeWithUsage>()
                });
            }
        }







        [Authorize]
        public async Task<IActionResult> MyRedemptions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var redemptions = await _context.UserRedemptions
                .Include(r => r.PointsReward)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();

            return View(redemptions);
        }



        [Authorize]
        public async Task<IActionResult> ClaimBirthdayBenefit()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                
                var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            // Birthday benefits don't require existing points - they give you points!

            // Check if user has birthday set
            if (!user.DateOfBirth.HasValue)
            {
                TempData["ErrorMessage"] = "Please add your birthday to your profile to claim birthday benefits!";
                return RedirectToAction("Index", "Profile");
            }

            // Check if it's the user's birthday (within the current month)
            var today = DateTime.UtcNow;
            var birthday = user.DateOfBirth.Value;
            var isBirthdayMonth = today.Month == birthday.Month;

            if (!isBirthdayMonth)
            {
                TempData["ErrorMessage"] = "Birthday benefits are only available during your birthday month!";
                return RedirectToAction("Index");
            }

            // Check if user already claimed birthday benefit this year
            var lastBirthdayClaim = await _context.UserPointsTransactions
                .Where(t => t.UserId == userId && 
                           t.Description.Contains("Birthday Benefit") && 
                           t.CreatedAt.Year == today.Year)
                .FirstOrDefaultAsync();

            if (lastBirthdayClaim != null)
            {
                TempData["ErrorMessage"] = "You have already claimed your birthday benefit for this year!";
                return RedirectToAction("Index");
            }

            // Give birthday benefits
            int birthdayPoints = 100; // 100 bonus points
            user.Points += birthdayPoints;
            user.TotalPointsEarned += birthdayPoints;

            // Create points transaction record
            var pointsTransaction = new UserPointsTransaction
            {
                UserId = userId,
                Points = birthdayPoints,
                Type = PointsTransactionType.Earned,
                Description = $"Birthday Benefit - Happy Birthday! {birthdayPoints} bonus points"
            };

            _context.UserPointsTransactions.Add(pointsTransaction);
            await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Happy Birthday! You've received {birthdayPoints} bonus points as a birthday gift! 🎂🎉";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while processing your birthday benefit. Please try again later.";
                return RedirectToAction("Index");
            }
        }


        // POST: /Deals/AssignPromoCode (Admin only)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPromoCode(string userId, int dealId, int maxUses = 1)
        {
            try
            {
                // Get the deal
                var deal = await _context.Deals.FindAsync(dealId);
                if (deal == null)
                {
                    return Json(new { success = false, message = "Promo code not found" });
                }

                // Get the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if user already has this promo code
                var existingUserPromoCode = await _context.UserPromoCodes
                    .FirstOrDefaultAsync(upc => upc.UserId == userId && upc.DealId == dealId);

                if (existingUserPromoCode != null)
                {
                    // Update existing promo code usage limit
                    existingUserPromoCode.MaxUses = maxUses;
                    existingUserPromoCode.IsActive = true;
                }
                else
                {
                    // Create new UserPromoCode
                    var userPromoCode = new UserPromoCode
                    {
                        UserId = userId,
                        DealId = dealId,
                        PromoCode = deal.PromoCode ?? "",
                        Title = deal.Title,
                        Description = deal.Description,
                        DiscountPercentage = deal.DiscountPercentage,
                        DiscountedPrice = deal.DiscountedPrice,
                        MinimumOrderAmount = deal.MinimumOrderAmount,
                        StartDate = deal.StartDate,
                        EndDate = deal.EndDate,
                        MaxUses = maxUses,
                        CurrentUses = 0,
                        IsActive = true,
                        SavedAt = DateTime.UtcNow,
                        IsUsed = false
                    };

                    _context.UserPromoCodes.Add(userPromoCode);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Promo code '{deal.PromoCode}' assigned to user successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error assigning promo code: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while assigning the promo code" });
            }
        }

    }

    public class DealsViewModel
    {
        public List<Deal> CurrentDeals { get; set; } = new();
        public List<Deal> FlashSales { get; set; } = new();
        public List<Deal> BundleOffers { get; set; } = new();
        public List<Deal> SeasonalDiscounts { get; set; } = new();
        public List<Deal> PromoCodes { get; set; } = new(); // Keep for backward compatibility
        public List<PromoCodeWithUsage> PromoCodesWithUsage { get; set; } = new();
        public ApplicationUser? User { get; set; }
        public int UserPoints { get; set; }
        public List<MenuItem> RedeemableItems { get; set; } = new();
    }

    public class PromoCodeWithUsage
    {
        public Deal Deal { get; set; } = null!;
        public int CurrentUses { get; set; }
        public int MaxUses { get; set; }
        
        public string UsageDisplay 
        { 
            get 
            {
                if (MaxUses == -1)
                    return "Unlimited";
                return $"{CurrentUses}/{MaxUses}";
            } 
        }
    }


}
