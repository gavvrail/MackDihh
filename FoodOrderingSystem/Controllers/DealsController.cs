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
                MemberDeals = deals.Where(d => d.RequiresMember).ToList(),
                StudentDiscounts = deals.Where(d => d.RequiresStudentVerification).ToList(),
                ReferralBonus = deals.FirstOrDefault(d => d.Type == DealType.ReferralBonus),
                User = user,
                IsMember = user?.IsMember == true && user.MemberExpiryDate > now,
                IsStudentVerified = user?.IsStudentVerified == true,
                UserPoints = user?.Points ?? 0,
                RedeemableItems = redeemableItems
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> MemberPurchase()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new MemberPurchaseViewModel
            {
                User = user,
                IsMember = user.IsMember && user.MemberExpiryDate > DateTime.UtcNow,
                CurrentPoints = user.Points
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PurchaseMember(MemberPurchaseViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.User = user;
                model.IsMember = user.IsMember && user.MemberExpiryDate > DateTime.UtcNow;
                model.CurrentPoints = user.Points;
                return View("MemberPurchase", model);
            }

            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddMonths((int)model.SelectedPlan);
            var amount = GetMemberPlanPrice(model.SelectedPlan);

            var subscription = new MemberSubscription
            {
                UserId = userId,
                Plan = model.SelectedPlan,
                Amount = amount,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                Status = SubscriptionStatus.Active
            };

            _context.MemberSubscriptions.Add(subscription);

            // Update user
            user.IsMember = true;
            user.MemberExpiryDate = endDate;
            user.LastMemberPurchaseDate = startDate;
            user.MemberPurchaseCount++;

            // Add bonus points based on plan
            int bonusPoints = model.SelectedPlan switch
            {
                MemberPlan.Monthly => 0,
                MemberPlan.Quarterly => 50,
                MemberPlan.Yearly => 200,
                _ => 0
            };

            if (bonusPoints > 0)
            {
                user.Points += bonusPoints;
                user.TotalPointsEarned += bonusPoints;
            }

            await _context.SaveChangesAsync();

            var bonusMessage = bonusPoints > 0 ? $" You also received {bonusPoints} bonus points!" : "";
            TempData["SuccessMessage"] = $"Congratulations! You are now a member until {endDate:MMMM dd, yyyy}. You'll earn points on every purchase!{bonusMessage}";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> StudentVerification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new StudentVerificationViewModel
            {
                User = user,
                IsVerified = user.IsStudentVerified
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitStudentVerification(StudentVerificationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.User = user;
                model.IsVerified = user.IsStudentVerified;
                return View("StudentVerification", model);
            }

            // Check if student ID already exists
            var existingStudent = await _context.Users
                .FirstOrDefaultAsync(u => u.StudentId == model.StudentId && u.Id != userId);
            
            if (existingStudent != null)
            {
                ModelState.AddModelError("StudentId", "This student ID is already registered.");
                model.User = user;
                model.IsVerified = user.IsStudentVerified;
                return View("StudentVerification", model);
            }

            // Update user with student information
            user.StudentId = model.StudentId;
            user.InstitutionName = model.InstitutionName;
            user.IsStudentVerified = true;
            user.StudentVerificationDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student verification submitted successfully! You now have access to student discounts.";
            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> ReferralProgram()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            // Generate referral code if user doesn't have one
            if (string.IsNullOrEmpty(user.ReferralCode))
            {
                user.ReferralCode = GenerateReferralCode();
                await _context.SaveChangesAsync();
            }

            var referrals = await _context.Users
                .Where(u => u.ReferredBy == user.ReferralCode)
                .ToListAsync();

            var viewModel = new ReferralProgramViewModel
            {
                User = user,
                Referrals = referrals,
                TotalCredits = user.ReferralCredits
            };

            return View(viewModel);
        }

        private decimal GetMemberPlanPrice(MemberPlan plan)
        {
            return plan switch
            {
                MemberPlan.Monthly => 29.99m,
                MemberPlan.Quarterly => 79.99m,
                MemberPlan.Yearly => 299.99m,
                _ => 29.99m
            };
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

            // Check if user is a member
            bool isMember = user.IsMember && user.MemberExpiryDate > DateTime.UtcNow;
            if (!isMember)
            {
                TempData["ErrorMessage"] = "You must be a member to claim birthday benefits!";
                return RedirectToAction("MemberPurchase");
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

        private string GenerateReferralCode()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
        public List<Deal> MemberDeals { get; set; } = new();
        public List<Deal> StudentDiscounts { get; set; } = new();
        public Deal? ReferralBonus { get; set; }
        public ApplicationUser? User { get; set; }
        public bool IsMember { get; set; }
        public bool IsStudentVerified { get; set; }
        public int UserPoints { get; set; }
        public List<MenuItem> RedeemableItems { get; set; } = new();
    }

    public class MemberPurchaseViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public bool IsMember { get; set; }
        public int CurrentPoints { get; set; }
        
        [Required(ErrorMessage = "Please select a membership plan")]
        public MemberPlan SelectedPlan { get; set; }
    }

    public class StudentVerificationViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public bool IsVerified { get; set; }
        
        [Required(ErrorMessage = "Student ID is required")]
        [StringLength(50, ErrorMessage = "Student ID cannot be longer than 50 characters")]
        public string StudentId { get; set; } = "";
        
        [Required(ErrorMessage = "Institution name is required")]
        [StringLength(100, ErrorMessage = "Institution name cannot be longer than 100 characters")]
        public string InstitutionName { get; set; } = "";
    }



    public class ReferralProgramViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<ApplicationUser> Referrals { get; set; } = new();
        public decimal TotalCredits { get; set; }
    }
}
