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

            var viewModel = new DealsViewModel
            {
                CurrentDeals = deals.Where(d => d.Type == DealType.LimitedTimeOffer).ToList(),
                FlashSales = deals.Where(d => d.IsFlashSale).ToList(),
                BundleOffers = deals.Where(d => d.Type == DealType.BundleOffer).ToList(),
                SeasonalDiscounts = deals.Where(d => d.IsSeasonal).ToList(),
                PromoCodes = deals.Where(d => d.Type == DealType.PromoCode).ToList(),
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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Congratulations! You are now a member until {endDate:MMMM dd, yyyy}. You'll earn points on every purchase!";
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

        private string GenerateReferralCode()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
