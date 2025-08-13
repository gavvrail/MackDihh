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
        public string RecaptchaSiteKey { get; }

        // --- 2. UPDATE THE CONSTRUCTOR TO RECEIVE NEW SERVICES ---
        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          ILogger<LoginModel> logger,
                          UserManager<ApplicationUser> userManager,
                          IConfiguration configuration,
                          RecaptchaService recaptchaService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;

            // Assign new services
            _recaptchaService = recaptchaService;
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
                if (userName.Contains("@"))
                {
                    var user = await _userManager.FindByEmailAsync(userName);
                    if (user != null)
                    {
                        userName = user.UserName;
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(userName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
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
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}