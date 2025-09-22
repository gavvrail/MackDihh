using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly FileUploadService _fileUploadService;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            FileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _fileUploadService = fileUploadService;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var profileViewModel = new ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Points = user.Points,
                TotalPointsEarned = user.TotalPointsEarned,
                TotalPointsRedeemed = user.TotalPointsRedeemed,
                IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user),
                IsAdmin = isAdmin
            };

            ViewBag.ProfilePhotoUrl = user.ProfilePhotoUrl;
            ViewBag.IsAdmin = isAdmin;
            return View(profileViewModel);
        }

        // GET: /Profile/Security
        public async Task<IActionResult> Security()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var securityViewModel = new SecurityViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user),
                IsPhoneNumberConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user),
                IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                LastLoginDate = user.LastLoginDate,
                LoginAttempts = user.LoginAttempts,
                LastLoginAttempt = user.LastLoginAttempt,
                IsBlocked = user.IsBlocked,
                BlockedUntil = user.BlockedUntil,
                BlockReason = user.BlockReason,
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
            };

            return View(securityViewModel);
        }

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.Address = model.Address;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update phone number using the proper Identity method
                var newPhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
                var currentPhoneNumber = user.PhoneNumber?.Trim() ?? string.Empty;
                
                if (newPhoneNumber != currentPhoneNumber)
                {
                    var phoneResult = await _userManager.SetPhoneNumberAsync(user, newPhoneNumber);
                    if (!phoneResult.Succeeded)
                    {
                        foreach (var error in phoneResult.Errors)
                        {
                            ModelState.AddModelError("", $"Phone number error: {error.Description}");
                        }
                        return View("Index", model);
                    }
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("Index", model);
        }

        // POST: /Profile/UploadProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (profilePicture == null || profilePicture.Length == 0)
                {
                    return Json(new { success = false, message = "No file selected" });
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _fileUploadService.DeleteFile(user.ProfilePhotoUrl);
                }

                // Upload new profile picture
                var imagePath = await _fileUploadService.UploadProfilePictureAsync(profilePicture, userId);
                
                // Update user profile
                user.ProfilePhotoUrl = imagePath;
                // Use context directly to ensure ProfilePhotoUrl is saved
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // Also update through UserManager to ensure Identity cache is updated
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, imagePath = imagePath, message = "Profile picture updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Profile/UploadCroppedProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCroppedProfilePicture([FromBody] CroppedImageModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (string.IsNullOrEmpty(model.ImageData))
                {
                    return Json(new { success = false, message = "No image data provided" });
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _fileUploadService.DeleteFile(user.ProfilePhotoUrl);
                }

                // Process and save cropped image
                var imagePath = await _fileUploadService.ProcessCroppedImageAsync(model.ImageData, userId, "profile");
                
                // Update user profile
                user.ProfilePhotoUrl = imagePath;
                // Use context directly to ensure ProfilePhotoUrl is saved
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // Also update through UserManager to ensure Identity cache is updated
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, imagePath = imagePath, message = "Profile picture updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Profile/OrderHistory
        public async Task<IActionResult> OrderHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // POST: /Profile/SendVerificationEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["InfoMessage"] = "Your email is already verified!";
                return RedirectToAction(nameof(Index));
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            var emailBody = EmailTemplates.GetEmailConfirmationTemplate(user.UserName ?? "User", HtmlEncoder.Default.Encode(callbackUrl ?? ""));
            await _emailSender.SendEmailAsync(user.Email ?? "", "Welcome to MackDihh! Please confirm your email", emailBody);

            TempData["SuccessMessage"] = "Verification email has been sent! Please check your inbox and click the confirmation link to verify your email address.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetReferralCode()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Generate referral code if user doesn't have one
            if (string.IsNullOrEmpty(user.ReferralCode))
            {
                // Generate a unique referral code based on username and user ID
                var baseCode = user.UserName?.ToUpper().Replace(" ", "") ?? "USER";
                var userIdSuffix = user.Id.Substring(0, 4).ToUpper();
                user.ReferralCode = $"{baseCode}{userIdSuffix}";
                
                // Ensure uniqueness
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == user.ReferralCode);
                if (existingUser != null)
                {
                    // If code exists, add timestamp
                    user.ReferralCode = $"{baseCode}{userIdSuffix}{DateTime.Now:MM}";
                }
                
                await _userManager.UpdateAsync(user);
            }

            TempData["SuccessMessage"] = $"Your referral code is: {user.ReferralCode}. Share this code with friends to earn 50 points per referral!";
            return RedirectToAction(nameof(Index));
        }
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        public string? LastName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        // Points information
        public int Points { get; set; } = 0;
        public int TotalPointsEarned { get; set; } = 0;
        public int TotalPointsRedeemed { get; set; } = 0;

        public bool IsEmailConfirmed { get; set; }
        public bool IsAdmin { get; set; } = false;
    }

    public class SecurityViewModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsPhoneNumberConfirmed { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int LoginAttempts { get; set; }
        public DateTime? LastLoginAttempt { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = "";
    }
} 