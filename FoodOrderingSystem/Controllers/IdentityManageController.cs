using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.Data;
using System.Security.Claims;
using System.Linq;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class IdentityManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly FileUploadService _fileUploadService;

        public IdentityManageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            FileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _fileUploadService = fileUploadService;
        }

        // POST: /IdentityManage/UploadProfilePicture
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

                // Log the upload attempt
                Console.WriteLine($"Uploading profile picture for user {userId}. File: {profilePicture.FileName}, Size: {profilePicture.Length}");

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _fileUploadService.DeleteFile(user.ProfilePhotoUrl);
                    Console.WriteLine($"Deleted old profile picture: {user.ProfilePhotoUrl}");
                }

                // Upload new profile picture
                var imagePath = await _fileUploadService.UploadProfilePictureAsync(profilePicture, userId);
                Console.WriteLine($"Uploaded new profile picture: {imagePath}");
                
                // Update user profile
                user.ProfilePhotoUrl = imagePath;
                // Use context directly to ensure ProfilePhotoUrl is saved
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // Also update through UserManager to ensure Identity cache is updated
                var updateResult = await _userManager.UpdateAsync(user);
                
                if (updateResult.Succeeded)
                {
                    // Refresh the user's sign-in to ensure the changes are reflected immediately
                    await _signInManager.RefreshSignInAsync(user);
                    Console.WriteLine($"Successfully updated user profile with new image path: {imagePath}");
                    return Json(new { success = true, imagePath = imagePath, message = "Profile picture updated successfully" });
                }
                else
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to update user profile: {errors}");
                    return Json(new { success = false, message = $"Failed to update profile: {errors}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading profile picture: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /IdentityManage/UploadCroppedProfilePicture
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

                Console.WriteLine($"Processing cropped profile picture for user {userId}");

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _fileUploadService.DeleteFile(user.ProfilePhotoUrl);
                    Console.WriteLine($"Deleted old profile picture: {user.ProfilePhotoUrl}");
                }

                // Process and save cropped image
                var imagePath = await _fileUploadService.ProcessCroppedImageAsync(model.ImageData, userId, "profile");
                Console.WriteLine($"Processed cropped profile picture: {imagePath}");
                
                // Update user profile
                user.ProfilePhotoUrl = imagePath;
                // Use context directly to ensure ProfilePhotoUrl is saved
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // Also update through UserManager to ensure Identity cache is updated
                var updateResult = await _userManager.UpdateAsync(user);
                
                if (updateResult.Succeeded)
                {
                    // Refresh the user's sign-in to ensure the changes are reflected immediately
                    await _signInManager.RefreshSignInAsync(user);
                    Console.WriteLine($"Successfully updated user profile with new cropped image path: {imagePath}");
                    return Json(new { success = true, imagePath = imagePath, message = "Profile picture updated successfully" });
                }
                else
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to update user profile: {errors}");
                    return Json(new { success = false, message = $"Failed to update profile: {errors}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing cropped profile picture: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
