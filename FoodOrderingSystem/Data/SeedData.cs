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
                    new MenuItem { Name = "Spicy Habanero Burger", Description = "A fiery habanero sauce with a crispy chicken patty. Only for a limited time!", Price = 9.99m, CategoryId = limitedTimeCategory.Id, ImageUrl = "/images/4.png" },
                    new MenuItem { Name = "Durian McFlurry", Description = "A local favorite! Creamy vanilla soft serve with real D24 durian puree.", Price = 5.99m, CategoryId = limitedTimeCategory.Id, ImageUrl = "/images/5.png" },

                    // Value Meals / Combos
                    new MenuItem { Name = "Big MackDihh Combo", Description = "Big MackDihh, World Famous Fries, and a medium Cola. This ultimate combo features our signature double-decker burger with special sauce, perfectly seasoned golden fries, and a refreshing medium cola. Perfect for satisfying your hunger with a complete meal that includes everything you love about MackDihh in one convenient package.", Price = 12.99m, CategoryId = combosCategory.Id, ImageUrl = "/images/9.png" },
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
                    new MenuItem { Name = "Cheeseburger Happy Meal", Description = "A cheeseburger, small fries, a drink, and a surprise toy. This complete kids meal includes a juicy cheeseburger with melted cheese, a small portion of our world-famous fries, a choice of drink (cola, orange juice, or milk), and an exciting surprise toy that will bring joy to any child. Perfect for families looking for a fun and nutritious meal option.", Price = 9.99m, CategoryId = happyMealsCategory.Id, ImageUrl = "/images/19.png" },
                    new MenuItem { Name = "Nuggets Happy Meal (4pcs)", Description = "4pcs McNuggets, small fries, a drink, and a surprise toy.", Price = 9.99m, CategoryId = happyMealsCategory.Id, ImageUrl = "/images/20.png" },

                    // Coffee & McCafe
                    new MenuItem { Name = "Cappuccino", Description = "A warm, frothy coffee made with fresh espresso and steamed milk. Our signature cappuccino is crafted with premium Arabica beans, perfectly steamed milk, and a rich layer of velvety foam. Each cup is carefully prepared by our trained baristas to ensure the perfect balance of espresso, milk, and foam. Served in a classic ceramic cup for the authentic coffee shop experience.", Price = 7.99m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/21.png" },
                    new MenuItem { Name = "Iced Latte", Description = "Chilled espresso with milk, served over ice.", Price = 8.49m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/22.png" },
                    new MenuItem { Name = "Chocolate Muffin", Description = "A rich and moist chocolate muffin.", Price = 4.99m, CategoryId = coffeeCategory.Id, ImageUrl = "/images/23.png" },

                    // Drinks
                    new MenuItem { Name = "Cola", Description = "A refreshing and bubbly classic.", Price = 1.99m, CategoryId = drinksCategory.Id, ImageUrl = "/images/24.png" },
                    new MenuItem { Name = "Iced Lemon Tea", Description = "Sweet and tangy, perfect for a hot day.", Price = 2.49m, CategoryId = drinksCategory.Id, ImageUrl = "/images/25.png" },
                    new MenuItem { Name = "Orange Juice", Description = "Freshly squeezed orange juice.", Price = 2.99m, CategoryId = drinksCategory.Id, ImageUrl = "/images/100plus.png" },

                    // Snacks & Sides
                    new MenuItem { Name = "World Famous Fries", Description = "Golden, crispy, and perfectly salted.", Price = 2.79m, CategoryId = snacksCategory.Id, ImageUrl = "/images/4.png" },
                    new MenuItem { Name = "Chicken McNuggets (6pcs)", Description = "Bite-sized pieces of seasoned chicken.", Price = 4.49m, CategoryId = snacksCategory.Id, ImageUrl = "/images/5.png" },
                    new MenuItem { Name = "Onion Rings", Description = "Crispy battered onion rings.", Price = 3.29m, CategoryId = snacksCategory.Id, ImageUrl = "/images/6.png" },

                    // Desserts
                    new MenuItem { Name = "McFlurry", Description = "Creamy vanilla soft serve with OREO® cookies.", Price = 3.29m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/4.png" },
                    new MenuItem { Name = "Apple Pie", Description = "A flaky, baked crust filled with warm apple filling.", Price = 1.49m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/5.png" },
                    new MenuItem { Name = "Chocolate Sundae", Description = "Vanilla soft serve topped with rich chocolate fudge.", Price = 3.99m, CategoryId = dessertsCategory.Id, ImageUrl = "/images/6.png" }
                );

                // Save all the new menu items to the database
                context.SaveChanges();

                // --- Seed Points Rewards ---
                if (!context.PointsRewards.Any())
                {
                    var rewards = new[]
                    {
                        new PointsReward
                        {
                            Title = "Free World Famous Fries",
                            Description = "Redeem your points for a free order of our World Famous Fries!",
                            PointsRequired = 50,
                            DiscountAmount = 2.79m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/mackfries.png",
                            IsActive = true,
                            MaxRedemptions = -1,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6)
                        },
                        new PointsReward
                        {
                            Title = "Free Big MackDihh",
                            Description = "Get a free Big MackDihh burger with your points! Our signature double-decker burger.",
                            PointsRequired = 200,
                            DiscountAmount = 7.99m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/15.png",
                            IsActive = true,
                            MaxRedemptions = -1,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6)
                        },
                        new PointsReward
                        {
                            Title = "Free Cappuccino",
                            Description = "Enjoy a free premium Cappuccino from our McCafe! Perfect for coffee lovers.",
                            PointsRequired = 150,
                            DiscountAmount = 7.99m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/21.png",
                            IsActive = true,
                            MaxRedemptions = -1,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6)
                        },
                        new PointsReward
                        {
                            Title = "Free Happy Meal",
                            Description = "Get a free Happy Meal for your little ones! Includes burger, fries, drink, and toy.",
                            PointsRequired = 250,
                            DiscountAmount = 9.99m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/Doraemon_meal.png",
                            IsActive = true,
                            MaxRedemptions = -1,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6)
                        },
                        new PointsReward
                        {
                            Title = "Free Family Bucket",
                            Description = "Redeem for a free MackDihh Family Bucket! Perfect for family gatherings.",
                            PointsRequired = 500,
                            DiscountAmount = 25.00m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/mackfamilybucket.png",
                            IsActive = true,
                            MaxRedemptions = 20, // Limited offer
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(3)
                        },
                        new PointsReward
                        {
                            Title = "Free McFlurry",
                            Description = "Enjoy a free McFlurry dessert! Creamy vanilla soft serve with OREO® cookies.",
                            PointsRequired = 80,
                            DiscountAmount = 3.29m,
                            DiscountPercentage = 0,
                            ImageUrl = "/images/4.png",
                            IsActive = true,
                            MaxRedemptions = -1,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6)
                        }
                    };

                    context.PointsRewards.AddRange(rewards);
                    context.SaveChanges();
                }

                // --- Seed Deals with Promo Codes ---
                if (!context.Deals.Any())
                {
                    var deals = new[]
                    {
                        new Deal
                        {
                            Title = "Welcome to MackDihh!",
                            Description = "Get 10% off your first order! Perfect for new customers trying our delicious food.",
                            Type = DealType.PromoCode,
                            PromoCode = "WELCOME10",
                            DiscountPercentage = 10.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6),
                            IsActive = true,
                            MaxUses = 1000,
                            MinimumOrderAmount = 0,
                            BadgeText = "New Customer",
                            BadgeColor = "success",
                            TermsAndConditions = "Valid for first-time customers only. Cannot be combined with other offers.",
                            ImageUrl = "/images/logo.png"
                        },
                        new Deal
                        {
                            Title = "Loyalty Special - Big MackDihh Combo",
                            Description = "Loyal customers get 15% off any order over RM20! Perfect for the Big MackDihh Combo.",
                            Type = DealType.PromoCode,
                            PromoCode = "LOYALTY15",
                            DiscountPercentage = 15.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(12),
                            IsActive = true,
                            MaxUses = -1, // Unlimited
                            MinimumOrderAmount = 20.0m,
                            BadgeText = "Loyalty Special",
                            BadgeColor = "primary",
                            TermsAndConditions = "For valued customers. Minimum order RM20.",
                            ImageUrl = "/images/9.png"
                        },
                        new Deal
                        {
                            Title = "Family Feast - MackDihh Family Bucket",
                            Description = "Save RM15 on orders over RM50! Perfect for our Family Bucket meals.",
                            Type = DealType.BundleOffer,
                            PromoCode = "FAMILY15",
                            DiscountPercentage = 0,
                            DiscountedPrice = 15.0m, // Fixed discount amount
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(3),
                            IsActive = true,
                            MaxUses = 500,
                            MinimumOrderAmount = 50.0m,
                            BadgeText = "Family Deal",
                            BadgeColor = "warning",
                            TermsAndConditions = "Minimum order RM50. Valid for 3 months.",
                            ImageUrl = "/images/mackfamilybucket.png"
                        },
                        new Deal
                        {
                            Title = "Flash Sale - Spicy Habanero Burger",
                            Description = "Limited time! Get 20% off our new Spicy Habanero Burger for the next 24 hours!",
                            Type = DealType.FlashSale,
                            PromoCode = "FLASH20",
                            DiscountPercentage = 20.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(1),
                            IsActive = true,
                            MaxUses = 200,
                            IsFlashSale = true,
                            BadgeText = "Flash Sale",
                            BadgeColor = "danger",
                            TermsAndConditions = "Limited time offer. Valid for 24 hours only.",
                            ImageUrl = "/images/4.png"
                        },
                        new Deal
                        {
                            Title = "Weekend Breakfast Special",
                            Description = "Enjoy 12% off on weekends! Perfect for our Sausage & Egg Muffin and Hotcakes.",
                            Type = DealType.SeasonalDiscount,
                            PromoCode = "WEEKEND12",
                            DiscountPercentage = 12.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(2),
                            IsActive = true,
                            MaxUses = -1,
                            IsSeasonal = true,
                            BadgeText = "Weekend Only",
                            BadgeColor = "secondary",
                            TermsAndConditions = "Valid Friday to Sunday only.",
                            ImageUrl = "/images/12.png"
                        },
                        new Deal
                        {
                            Title = "Happy Meal Fun - Doraemon Special",
                            Description = "Kids love our Doraemon Happy Meal! Get 10% off any Happy Meal order.",
                            Type = DealType.BundleOffer,
                            PromoCode = "HAPPY10",
                            DiscountPercentage = 10.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(6),
                            IsActive = true,
                            MaxUses = 300,
                            MinimumOrderAmount = 0,
                            BadgeText = "Kids Special",
                            BadgeColor = "info",
                            TermsAndConditions = "Valid for Happy Meal orders only.",
                            ImageUrl = "/images/Doraemon_meal.png"
                        },
                        new Deal
                        {
                            Title = "McCafe Coffee Break",
                            Description = "Enjoy 15% off our premium McCafe drinks! Perfect for your coffee break.",
                            Type = DealType.PromoCode,
                            PromoCode = "COFFEE15",
                            DiscountPercentage = 15.0m,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(4),
                            IsActive = true,
                            MaxUses = 400,
                            MinimumOrderAmount = 15.0m,
                            BadgeText = "McCafe Special",
                            BadgeColor = "dark",
                            TermsAndConditions = "Valid for McCafe items only. Minimum order RM15.",
                            ImageUrl = "/images/21.png"
                        }
                    };

                    context.Deals.AddRange(deals);
                    context.SaveChanges();
                }
                
                // Update PointsPerItem for existing menu items based on their prices
                UpdateMenuItemsPoints(context);
            }
        }
        
        private static void UpdateMenuItemsPoints(ApplicationDbContext context)
        {
            var menuItems = context.MenuItems.Where(m => m.PointsPerItem == 0).ToList();
            
            foreach (var item in menuItems)
            {
                // Set PointsPerItem based on price (1 point = RM 1.00, rounded up)
                item.PointsPerItem = (int)Math.Ceiling(item.Price);
            }
            
            if (menuItems.Any())
            {
                context.SaveChanges();
            }
        }
    }
}
