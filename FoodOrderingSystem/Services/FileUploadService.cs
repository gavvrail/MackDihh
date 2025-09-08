using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace FoodOrderingSystem.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadProfilePictureAsync(IFormFile file, string userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file provided");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Invalid file type. Only JPG, PNG, and GIF are allowed.");

                if (file.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("File size too large. Maximum size is 5MB.");

                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "uploads", "profiles");
                Directory.CreateDirectory(uploadPath);

                var fileName = $"profile_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/images/uploads/profiles/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string> UploadMenuItemImageAsync(IFormFile file, int menuItemId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file provided");

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Invalid file type. Only JPG, PNG, and GIF are allowed.");

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("File size too large. Maximum size is 10MB.");

                // Create upload directory if it doesn't exist
                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "uploads", "menu-items");
                Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileName = $"menu_{menuItemId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Process and save the image
                using (var stream = file.OpenReadStream())
#if WINDOWS
                using (var image = Image.FromStream(stream))
                {
                    // Resize image to standard menu item size (400x300)
                    var resizedImage = ResizeImage(image, 400, 300);
                    resizedImage.Save(filePath, ImageFormat.Jpeg);
                }
#else
                // For non-Windows platforms, copy file directly without processing
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
#endif

                // Return the relative path for database storage
                return await Task.FromResult($"/images/uploads/menu-items/{fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading menu item image for item {MenuItemId}", menuItemId);
                throw;
            }
        }

        public async Task<string> ProcessCroppedImageAsync(string base64Image, string userId, string type = "profile")
        {
            try
            {
                var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
                var imageBytes = Convert.FromBase64String(base64Data);

                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "uploads", type == "profile" ? "profiles" : "menu-items");
                Directory.CreateDirectory(uploadPath);

                var fileName = $"{type}_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                var filePath = Path.Combine(uploadPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                return $"/images/uploads/{(type == "profile" ? "profiles" : "menu-items")}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cropped image for user {UserId}", userId);
                throw;
            }
        }

#if WINDOWS
        private static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
#endif

        public void DeleteFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
            }
        }
    }
}
