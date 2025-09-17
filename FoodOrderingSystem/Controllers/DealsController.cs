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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = userId != null ? await _context.Users.FindAsync(userId) : null;
            
            var now = DateTime.UtcNow;
            var deals = await _context.Deals
                .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now)
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

            // Get user's available promo codes (not used and within usage limits)
            var availablePromoCodes = new List<Deal>();
            if (user != null)
            {
                var userPromoCodes = await _context.UserPromoCodes
                    .Where(upc => upc.UserId == userId && !upc.IsUsed && upc.IsActive)
                    .ToListAsync();

                var promoCodeDeals = deals.Where(d => d.Type == DealType.PromoCode).ToList();
                
                foreach (var deal in promoCodeDeals)
                {
                    var userPromoCode = userPromoCodes.FirstOrDefault(upc => upc.DealId == deal.Id);
                    if (userPromoCode != null && userPromoCode.CurrentUses < userPromoCode.MaxUses)
                    {
                        availablePromoCodes.Add(deal);
                    }
                }
            }

            var viewModel = new DealsViewModel
            {
                CurrentDeals = deals.Where(d => d.Type == DealType.LimitedTimeOffer).ToList(),
                FlashSales = deals.Where(d => d.IsFlashSale).ToList(),
                BundleOffers = deals.Where(d => d.Type == DealType.BundleOffer).ToList(),
                SeasonalDiscounts = deals.Where(d => d.IsSeasonal).ToList(),
                PromoCodes = availablePromoCodes, // Only show available promo codes for the user
                User = user,
                UserPoints = user?.Points ?? 0,
                RedeemableItems = redeemableItems
            };

            return View(viewModel);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            // Check if user has points to claim birthday benefits
            if (user.Points < 100)
            {
                TempData["ErrorMessage"] = "You need at least 100 points to claim birthday benefits!";
                return RedirectToAction(nameof(Index));
            }

            // Check if user has birthday set
            if (!user.DateOfBirth.HasValue)
            {
                TempData["ErrorMessage"] = "Please add your birthday to your profile to claim birthday benefits!";
                return RedirectToAction("Index", "Profile");
            }

            // Check if it's the user's birthday (within the current month)
            var today = DateTime.UtcNow;
            var birthday = user.DateOfBirth.Value;
            var isBirthdayMonth = today.Month == birthday.Month && today.Year == birthday.Year;

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
        public List<Deal> PromoCodes { get; set; } = new();
        public ApplicationUser? User { get; set; }
        public int UserPoints { get; set; }
        public List<MenuItem> RedeemableItems { get; set; } = new();
    }


}
