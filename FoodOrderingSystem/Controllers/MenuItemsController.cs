using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace FoodOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuItemsController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create([Bind("Id,Name,ImageUrl,Description,Price,CategoryId,IsAvailable,IsFeatured,PreparationTimeMinutes,Calories,Allergens")] MenuItem menuItem)
        {
            // Debug: Log the incoming data
            System.Diagnostics.Debug.WriteLine($"Creating menu item: Name={menuItem.Name}, Price={menuItem.Price}, CategoryId={menuItem.CategoryId}");
            
            // Debug: Log the model state
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ImageUrl,Description,Price,CategoryId,IsAvailable,IsFeatured,PreparationTimeMinutes,Calories,Allergens")] MenuItem menuItem)
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
    }
}
