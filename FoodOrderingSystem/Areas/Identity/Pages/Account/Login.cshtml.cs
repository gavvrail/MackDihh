#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
// --- ADD THESE USING STATEMENTS ---
using FoodOrderingSystem.Services;
using Microsoft.Extensions.Configuration;

namespace FoodOrderingSystem.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        // --- 1. ADD NEW FIELDS FOR RECAPTCHA ---
        private readonly RecaptchaService _recaptchaService;
        private readonly LoginSecurityService _loginSecurityService;
        public string RecaptchaSiteKey { get; }

        // --- 2. UPDATE THE CONSTRUCTOR TO RECEIVE NEW SERVICES ---
        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          ILogger<LoginModel> logger,
                          UserManager<ApplicationUser> userManager,
                          IConfiguration configuration,
                          RecaptchaService recaptchaService,
                          LoginSecurityService loginSecurityService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;

            // Assign new services
            _recaptchaService = recaptchaService;
            _loginSecurityService = loginSecurityService;
            RecaptchaSiteKey = configuration["RecaptchaSettings:SiteKey"];
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Email or Username")]
            public string EmailOrUsername { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // --- 3. ADD VALIDATION LOGIC AT THE BEGINNING OF OnPostAsync ---
            var recaptchaToken = Request.Form["g-recaptcha-response"];
            var isRecaptchaValid = await _recaptchaService.Validate(recaptchaToken);
            if (!isRecaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "CAPTCHA validation failed. Please try again.");
                return Page(); // Stop processing if CAPTCHA fails
            }

            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var userName = Input.EmailOrUsername;
                ApplicationUser user = null;

                // Find user by email or username
                if (userName.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(userName);
                    if (user != null)
                    {
                        userName = user.UserName;
                    }
                }
                else
                {
                    user = await _userManager.FindByNameAsync(userName);
                }

                // Check if user exists and is blocked
                if (user != null)
                {
                    var isBlocked = await _loginSecurityService.IsUserBlockedAsync(user.Id);
                    if (isBlocked)
                    {
                        var blockExpiry = await _loginSecurityService.GetBlockExpiryAsync(user.Id);
                        var errorMessage = "Your account has been temporarily blocked due to multiple failed login attempts.";
                        if (blockExpiry.HasValue)
                        {
                            errorMessage += $" You can try again after {blockExpiry.Value:MMM dd, yyyy 'at' HH:mm}.";
                        }
                        ModelState.AddModelError(string.Empty, errorMessage);
                        return Page();
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(userName, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Record successful login
                    if (user != null)
                    {
                        await _loginSecurityService.RecordSuccessfulLoginAsync(user.Id);
                    }

                    _logger.LogInformation("User {UserName} logged in successfully. Remember Me: {RememberMe}", userName, Input.RememberMe);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Record failed login attempt
                    if (user != null)
                    {
                        await _loginSecurityService.RecordFailedLoginAttemptAsync(user.Id);
                        
                        // Check if user is now blocked after this failed attempt
                        var isNowBlocked = await _loginSecurityService.IsUserBlockedAsync(user.Id);
                        if (isNowBlocked)
                        {
                            var blockExpiry = await _loginSecurityService.GetBlockExpiryAsync(user.Id);
                            var errorMessage = "Too many failed login attempts. Your account has been temporarily blocked.";
                            if (blockExpiry.HasValue)
                            {
                                errorMessage += $" You can try again after {blockExpiry.Value:MMM dd, yyyy 'at' HH:mm}.";
                            }
                            ModelState.AddModelError(string.Empty, errorMessage);
                            return Page();
                        }
                        else
                        {
                            // Show remaining attempts warning
                            var remainingAttempts = await _loginSecurityService.GetRemainingAttemptsAsync(user.Id);
                            if (remainingAttempts <= 3 && remainingAttempts > 0)
                            {
                                var warningMessage = $"Invalid login attempt. You have {remainingAttempts} more {(remainingAttempts == 1 ? "try" : "tries")} left before your account is blocked.";
                                ModelState.AddModelError(string.Empty, warningMessage);
                                return Page();
                            }
                        }
                    }

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}