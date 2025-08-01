using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace FoodOrderingSystem.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if data already exists to prevent re-seeding
                if (context.Categories.Any())
                {
                    return;   // DB has been seeded
                }

                // --- Create Categories ---
                var limitedTimeCategory = new Category { Name = "Limited Time Offers" };
                var combosCategory = new Category { Name = "Value Meals / Combos" };
                var breakfastCategory = new Category { Name = "Breakfast Menu" };
                var burgersCategory = new Category { Name = "Burgers & Sandwiches" };
                var happyMealsCategory = new Category { Name = "Happy Meals" };
                var coffeeCategory = new Category { Name = "Coffee & McCafe" };
                var drinksCategory = new Category { Name = "Drinks" };
                var snacksCategory = new Category { Name = "Snacks & Sides" };
                var dessertsCategory = new Category { Name = "Desserts" };

                context.Categories.AddRange(
                    limitedTimeCategory,
                    combosCategory,
                    breakfastCategory,
                    burgersCategory,
                    happyMealsCategory,
                    coffeeCategory,
                    drinksCategory,
                    snacksCategory,
                    dessertsCategory
                );
                context.SaveChanges(); // Save categories to get their IDs

                // --- Create Menu Items ---
                context.MenuItems.AddRange(
                    // Limited Time Offers
                    new MenuItem { Name = "Spicy Habanero Burger", Description = "A fiery habanero sauce with a crispy chicken patty. Only for a limited time!", Price = 9.99m, CategoryId = limitedTimeCategory.Id, ImageUrl = "/images/7.png" },
                    new MenuItem { Name = "Durian McFlurry", Description = "A local favorite! Creamy vanilla soft serve with real D24 durian puree.", Price = 5.99m, CategoryId = limitedTimeCategory.Id, ImageUrl = "/images/8.png" },

                    // Value Meals / Combos
                    new MenuItem { Name = "Big MackDihh Combo", Description = "Big MackDihh, World Famous Fries, and a medium Cola.", Price = 12.99m, CategoryId = combosCategory.Id, ImageUrl = "/images/9.png" },
                    new MenuItem { Name = "Crispy Chicken Combo", Description = "Crispy Chicken Sandwich, World Famous Fries, and a medium Cola.", Price = 11.49m, CategoryId = combosCategory.Id, ImageUrl = "/images/10.png" },
                    new MenuItem { Name = "McNuggets Combo (9pcs)", Description = "9pcs Chicken McNuggets, World Famous Fries, and a medium Cola.", Price = 13.49m, CategoryId = combosCategory.Id, ImageUrl = "/images/11.png" },

                    // Breakfast Menu (Available 6 AM - 11 AM)
                    new MenuItem { Name = "Sausage & Egg Muffin", Description = "A savory sausage patty and a freshly cracked egg on a toasted English muffin.", Price = 6.99m, CategoryId = breakfastCategory.Id, ImageUrl = "/images/12.png" },
                    new MenuItem { Name = "Hotcakes (2pcs)", Description = "Two fluffy hotcakes served with butter and sweet syrup.", Price = 5.49m, CategoryId = breakfastCategory.Id, ImageUrl = "/images/13.png" },
                    new MenuItem { Name = "Hash Brown", Description = "Crispy, golden-brown shredded potato patty.", Price = 2.99m, CategoryId = breakfastCategory.Id, ImageUrl = "/images/14.png" },

                    // Burgers & Sandwiches
                    new MenuItem { Name = "Big MackDihh", Description = "Our signature double-decker burger with special sauce.", Price = 7.99m, CategoryId = burgersCategory.Id, ImageUrl = "/images/15.png" },
                    new MenuItem { Name = "Crispy Chicken Sandwich", Description = "A juicy, crispy chicken fillet on a toasted potato bun.", Price = 6.49m, CategoryId = burgersCategory.Id, ImageUrl = "/images/16.png" },
                    new MenuItem { Name = "Double Cheeseburger", Description = "Two beef patties with melted cheese, pickles, and onions.", Price = 8.49m, CategoryId = burgersCategory.Id, ImageUrl = "/images/17.png" },
                    new MenuItem { Name = "Fish Fillet", Description = "Flaky white fish fillet, topped with tartar sauce.", Price = 5.99m, CategoryId = burgersCategory.Id, ImageUrl = "/images/18.png" },

                    // Happy Meals
                    new MenuItem { Name = "Cheeseburger Happy Meal", Description = "A cheeseburger, small fries, a drink, and a surprise toy.", Price = 9.99m, CategoryId = happyMealsCategory.Id, ImageUrl = "/images/19.png" },
                    new MenuItem { Name = "Nuggets Happy Meal (4pcs)", Description = "4pcs McNuggets, small fries, a drink, and a surprise toy.", Price = 9.99m, CategoryId = happyMealsCategory.Id, ImageUrl = "/images/20.png" },

                    // Coffee & McCafe
                    new MenuItem { Name = "Cappuccino", Description = "A warm, frothy coffee made with fresh espresso and steamed milk.", Price = 7.99m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/21.png" },
                    new MenuItem { Name = "Iced Latte", Description = "Chilled espresso with milk, served over ice.", Price = 8.49m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/22.png" },
                    new MenuItem { Name = "Chocolate Muffin", Description = "A rich and moist chocolate muffin.", Price = 4.99m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/23.png" },

                    // Drinks
                    new MenuItem { Name = "Cola", Description = "A refreshing and bubbly classic.", Price = 1.99m, CategoryId = drinksCategory.Id, ImageUrl = "/images/24.png" },
                    new MenuItem { Name = "Iced Lemon Tea", Description = "Sweet and tangy, perfect for a hot day.", Price = 2.49m, CategoryId = drinksCategory.Id, ImageUrl = "/images/25.png" },
                    new MenuItem { Name = "Orange Juice", Description = "Freshly squeezed orange juice.", Price = 2.99m, CategoryId = drinksCategory.Id, ImageUrl = "/images/26.png" },

                    // Snacks & Sides
                    new MenuItem { Name = "World Famous Fries", Description = "Golden, crispy, and perfectly salted.", Price = 2.79m, CategoryId = snacksCategory.Id, ImageUrl = "/images/27.png" },
                    new MenuItem { Name = "Chicken McNuggets (6pcs)", Description = "Bite-sized pieces of seasoned chicken.", Price = 4.49m, CategoryId = snacksCategory.Id, ImageUrl = "/images/28.png" },
                    new MenuItem { Name = "Onion Rings", Description = "Crispy battered onion rings.", Price = 3.29m, CategoryId = snacksCategory.Id, ImageUrl = "/images/29.png" },

                    // Desserts
                    new MenuItem { Name = "McFlurry", Description = "Creamy vanilla soft serve with OREO® cookies.", Price = 3.29m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/30.png" },
                    new MenuItem { Name = "Apple Pie", Description = "A flaky, baked crust filled with warm apple filling.", Price = 1.49m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/31.png" },
                    new MenuItem { Name = "Chocolate Sundae", Description = "Vanilla soft serve topped with rich chocolate fudge.", Price = 3.99m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/32.png" }
                );

                // Save all the new menu items to the database
                context.SaveChanges();
            }
        }
    }
}
