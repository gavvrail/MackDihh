#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }
        public bool CanChangeUsername { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Username")]
            public string NewUsername { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                NewUsername = userName,
                PhoneNumber = phoneNumber
            };

            if (user.LastUsernameChangeDate.HasValue && user.LastUsernameChangeDate.Value.AddMonths(1) > DateTime.UtcNow)
            {
                if (user.UsernameChangeCount >= 3)
                {
                    CanChangeUsername = false;
                }
                else
                {
                    CanChangeUsername = true;
                }
            }
            else
            {
                CanChangeUsername = true;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            await LoadAsync(user);
            if (CanChangeUsername && Input.NewUsername != user.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(Input.NewUsername);
                if (existingUser != null)
                {
                    StatusMessage = "Error: This username is already taken. Please choose another one.";
                    return RedirectToPage();
                }

                var setUsernameResult = await _userManager.SetUserNameAsync(user, Input.NewUsername);
                if (!setUsernameResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set username.";
                    return RedirectToPage();
                }

                // --- THIS IS THE NEW FIX ---
                // Manually update the security stamp to ensure the cookie gets refreshed.
                await _userManager.UpdateSecurityStampAsync(user);
                // --- END OF NEW FIX ---

                if (user.LastUsernameChangeDate.HasValue && user.LastUsernameChangeDate.Value.AddMonths(1) > DateTime.UtcNow)
                {
                    user.UsernameChangeCount++;
                }
                else
                {
                    user.UsernameChangeCount = 1;
                }
                user.LastUsernameChangeDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
