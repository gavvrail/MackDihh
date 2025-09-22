using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;

namespace FoodOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;

        public MenuItemsController(ApplicationDbContext context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // GET: MenuItems
        public async Task<IActionResult> Index()
        {
            // Add debugging to see what's in the database
            var menuItems = await _context.MenuItems.Include(m => m.Category).ToListAsync();
            foreach (var item in menuItems)
            {
                System.Diagnostics.Debug.WriteLine($"MenuItem {item.Id}: {item.Name} - RM{item.Price}");
            }
            return View(menuItems);
        }

        // GET: MenuItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem == null) return NotFound();
            return View(menuItem);
        }

        // GET: MenuItems/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: MenuItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ImageUrl,Description,Price,CategoryId,IsAvailable,IsFeatured,PreparationTimeMinutes,Calories,Allergens,PointsPerItem")] MenuItem menuItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure CategoryId is valid
                    var categoryExists = await _context.Categories.AnyAsync(c => c.Id == menuItem.CategoryId);
                    if (!categoryExists)
                    {
                        ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", menuItem.CategoryId);
                        return View(menuItem);
                    }

                    _context.Add(menuItem);
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Menu item created successfully with ID: {menuItem.Id}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving menu item: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while saving the item. Please try again.");
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        /* Debug actions removed for production
        [AllowAnonymous]
        public async Task<IActionResult> DebugMenuItems()
        {
            try
            {
                var menuItems = await _context.MenuItems.Include(m => m.Category).ToListAsync();
                var categories = await _context.Categories.ToListAsync();
                
                var result = new
                {
                    TotalMenuItems = menuItems.Count,
                    TotalCategories = categories.Count,
                    MenuItems = menuItems.Select(m => new { m.Id, m.Name, m.Price, Category = m.Category?.Name, m.ImageUrl }),
                    Categories = categories.Select(c => new { c.Id, c.Name })
                };
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // Action to fix missing images (remove in production)
        [AllowAnonymous]
        public async Task<IActionResult> FixMissingImages()
        {
            try
            {
                var menuItems = await _context.MenuItems.ToListAsync();
                var updatedCount = 0;
                
                foreach (var item in menuItems)
                {
                    if (string.IsNullOrEmpty(item.ImageUrl) || !item.ImageUrl.StartsWith("/images/"))
                    {
                        item.ImageUrl = "/images/4.png"; // Use a default image
                        updatedCount++;
                    }
                }
                
                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
                
                return Json(new { 
                    Success = true, 
                    UpdatedCount = updatedCount,
                    Message = $"Updated {updatedCount} menu items with default images"
                });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // Test action to verify database connection and add a test item (remove in production)
        [AllowAnonymous]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                // Test 1: Check if we can connect to the database
                var categories = await _context.Categories.ToListAsync();
                var menuItems = await _context.MenuItems.ToListAsync();
                
                // Test 2: Try to add a test menu item
                var testItem = new MenuItem
                {
                    Name = "Test Item - " + DateTime.Now.ToString("HH:mm:ss"),
                    Description = "This is a test item to verify database connection",
                    Price = 9.99m,
                    CategoryId = categories.FirstOrDefault()?.Id ?? 1,
                    IsAvailable = true,
                    IsFeatured = false,
                    PreparationTimeMinutes = 10,
                    ImageUrl = "/images/4.png"
                };
                
                _context.MenuItems.Add(testItem);
                await _context.SaveChangesAsync();
                
                var result = new
                {
                    Success = true,
                    Message = "Database connection test successful!",
                    TestItemId = testItem.Id,
                    TestItemName = testItem.Name,
                    TotalCategories = categories.Count,
                    TotalMenuItems = menuItems.Count + 1,
                    Categories = categories.Select(c => new { c.Id, c.Name }),
                    RecentMenuItems = await _context.MenuItems.OrderByDescending(m => m.Id).Take(5).Select(m => new { m.Id, m.Name, m.Price, Category = m.Category!.Name }).ToListAsync()
                };
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { 
                    Success = false, 
                    Error = ex.Message, 
                    StackTrace = ex.StackTrace 
                });
            }
        }
        */

        // GET: MenuItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null) return NotFound();
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", menuItem.CategoryId);
            return View(menuItem);
        }



        // POST: MenuItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ImageUrl,Description,Price,CategoryId,IsAvailable,IsFeatured,PreparationTimeMinutes,Calories,Allergens,PointsPerItem")] MenuItem menuItem)
        {
            if (id != menuItem?.Id) 
            {
                return NotFound();
            }

            // Check if CategoryId is missing and use the existing value
            if (menuItem.CategoryId == 0)
            {
                var existingItem = await _context.MenuItems.FindAsync(id);
                if (existingItem != null)
                {
                    menuItem.CategoryId = existingItem.CategoryId;
                }
            }

            // Clear the Category validation error if CategoryId is valid
            if (menuItem.CategoryId > 0)
            {
                ModelState.Remove("Category");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMenuItem = await _context.MenuItems.FindAsync(id);
                    if (existingMenuItem == null)
                    {
                        return NotFound();
                    }

                    // Update the properties explicitly
                    existingMenuItem.Name = menuItem.Name ?? existingMenuItem.Name;
                    existingMenuItem.ImageUrl = menuItem.ImageUrl;
                    existingMenuItem.Description = menuItem.Description ?? existingMenuItem.Description;
                    existingMenuItem.Price = menuItem.Price;
                    existingMenuItem.CategoryId = menuItem.CategoryId;
                    existingMenuItem.IsAvailable = menuItem.IsAvailable;
                    existingMenuItem.IsFeatured = menuItem.IsFeatured;
                    existingMenuItem.PreparationTimeMinutes = menuItem.PreparationTimeMinutes;
                    existingMenuItem.Calories = menuItem.Calories;
                    existingMenuItem.Allergens = menuItem.Allergens;
                    existingMenuItem.PointsPerItem = menuItem.PointsPerItem;

                    _context.MenuItems.Update(existingMenuItem);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuItemExists(menuItem.Id)) return NotFound();
                    else throw;
                }
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", menuItem?.CategoryId);
            return View(menuItem);
        }



        // GET: MenuItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem == null) return NotFound();
            return View(menuItem);
        }

        // POST: MenuItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: MenuItems/BatchDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchDelete(int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No items were selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            var itemsToDelete = await _context.MenuItems.Where(m => selectedIds.Contains(m.Id)).ToListAsync();
            if (itemsToDelete.Count == 0)
            {
                TempData["ErrorMessage"] = "Selected items could not be found.";
                return RedirectToAction(nameof(Index));
            }

            _context.MenuItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted {itemsToDelete.Count} menu item(s).";
            return RedirectToAction(nameof(Index));
        }

        private bool MenuItemExists(int id)
        {
            return _context.MenuItems.Any(e => e.Id == id);
        }

        // POST: MenuItems/UploadImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile image, int menuItemId)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return Json(new { success = false, message = "No file selected" });
                }

                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item not found" });
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(menuItem.ImageUrl))
                {
                    _fileUploadService.DeleteFile(menuItem.ImageUrl);
                }

                // Upload new image
                var imagePath = await _fileUploadService.UploadMenuItemImageAsync(image, menuItemId);
                
                // Update menu item
                menuItem.ImageUrl = imagePath;
                await _context.SaveChangesAsync();

                return Json(new { success = true, imagePath = imagePath, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: MenuItems/UploadCroppedImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCroppedImage([FromBody] CroppedImageModel model, int menuItemId)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item not found" });
                }

                if (string.IsNullOrEmpty(model.ImageData))
                {
                    return Json(new { success = false, message = "No image data provided" });
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(menuItem.ImageUrl))
                {
                    _fileUploadService.DeleteFile(menuItem.ImageUrl);
                }

                // Process and save cropped image
                var imagePath = await _fileUploadService.ProcessCroppedImageAsync(model.ImageData, menuItemId.ToString(), "menu");
                
                // Update menu item
                menuItem.ImageUrl = imagePath;
                await _context.SaveChangesAsync();

                return Json(new { success = true, imagePath = imagePath, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
