using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace FoodOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.MenuItems)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(categories);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading categories. Please try again.";
                return View(new List<Category>());
            }
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Category ID is required.";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(category);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading category details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if category name already exists
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        return View(category);
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                return View(category);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the category. Please try again.";
                return View(category);
            }
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Category ID is required.";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(category);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                TempData["ErrorMessage"] = "Invalid category ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Check if category name already exists (excluding current category)
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != id);
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                return View(category);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the category. Please try again.";
                    return View(category);
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the category. Please try again.";
                return View(category);
            }
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Category ID is required.";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(category);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(c => c.Id == id);
                
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Check if category has menu items
                if (category.MenuItems.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it contains {category.MenuItems.Count} menu item(s). Please reassign or delete the menu items first.";
                    return RedirectToAction(nameof(Index));
                }
                
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
} 