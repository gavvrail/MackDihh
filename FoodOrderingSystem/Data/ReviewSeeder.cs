using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Data
{
    public static class ReviewSeeder
    {
        private static readonly Random _random = new Random();
        
        // Sample customer data
        private static readonly List<(string FirstName, string LastName, string Email, string UserName)> SampleCustomers = new()
        {
            ("Sarah", "Johnson", "sarah.johnson@email.com", "sarahj"),
            ("Michael", "Chen", "michael.chen@email.com", "mikec"),
            ("Emily", "Davis", "emily.davis@email.com", "emilyd"),
            ("James", "Wilson", "james.wilson@email.com", "jamesw"),
            ("Jessica", "Brown", "jessica.brown@email.com", "jessicab"),
            ("David", "Miller", "david.miller@email.com", "davidm"),
            ("Amanda", "Garcia", "amanda.garcia@email.com", "amandag"),
            ("Ryan", "Taylor", "ryan.taylor@email.com", "ryant"),
            ("Lisa", "Anderson", "lisa.anderson@email.com", "lisaa"),
            ("Kevin", "Thomas", "kevin.thomas@email.com", "kevint"),
            ("Michelle", "Lee", "michelle.lee@email.com", "michellee"),
            ("Daniel", "White", "daniel.white@email.com", "danielw"),
            ("Rachel", "Martinez", "rachel.martinez@email.com", "rachelm"),
            ("Christopher", "Clark", "christopher.clark@email.com", "chrisc"),
            ("Ashley", "Rodriguez", "ashley.rodriguez@email.com", "ashleyr")
        };

        // Sample review templates by category
        private static readonly Dictionary<string, List<(int Rating, string Comment)>> ReviewTemplates = new()
        {
            ["Burgers"] = new()
            {
                (5, "Absolutely amazing! The Big MackDihh is my go-to burger. Perfect balance of flavors and always fresh."),
                (4, "Really good burger, juicy and flavorful. The special sauce makes all the difference!"),
                (5, "Best burger in town! The double-decker style is genius and the taste is incredible."),
                (4, "Solid choice for a quick meal. The crispy chicken sandwich is always cooked perfectly."),
                (3, "Good burger but sometimes the bun gets a bit soggy. Still tasty though."),
                (5, "The double cheeseburger is my favorite! Two patties, melted cheese - pure perfection."),
                (4, "Fish fillet is surprisingly good! Light and flaky with great tartar sauce."),
                (5, "Love coming here for burgers. Consistent quality and great value for money.")
            },
            ["Combos"] = new()
            {
                (5, "The combo deals are fantastic! Great value and the fries are always crispy."),
                (4, "Big MackDihh combo is my regular order. Everything tastes fresh and the portion is perfect."),
                (5, "McNuggets combo is amazing! 9 pieces are more than enough and they're so crispy."),
                (4, "Crispy chicken combo hits the spot every time. The drink selection is good too."),
                (3, "Good combo deal, though sometimes the fries could be hotter. Overall satisfied."),
                (5, "Best value for money! The combos are filling and delicious."),
                (4, "Love the convenience of getting everything in one order. Quality is consistent.")
            },
            ["Breakfast"] = new()
            {
                (5, "Perfect breakfast option! The sausage & egg muffin is my morning fuel."),
                (4, "Hotcakes are fluffy and delicious. Great way to start the day!"),
                (5, "Hash browns are crispy on the outside, fluffy inside. Breakfast perfection!"),
                (3, "Good breakfast menu but wish they served until later. Quality is decent."),
                (4, "The English muffin is toasted perfectly and the egg is always fresh."),
                (5, "Best breakfast in the area! Quick service and everything tastes homemade.")
            },
            ["Happy Meals"] = new()
            {
                (5, "My kids absolutely love the Happy Meals! The toys are always exciting."),
                (4, "Great portion size for children. The nuggets are a hit with my little ones."),
                (5, "Perfect family meal option. Kids are happy and parents are satisfied too!"),
                (4, "The cheeseburger Happy Meal is my daughter's favorite. Good quality ingredients."),
                (3, "Kids enjoy it but wish there were more healthy drink options."),
                (5, "Excellent value for families. The surprise toys keep the kids coming back!")
            },
            ["Coffee"] = new()
            {
                (5, "The cappuccino here rivals any coffee shop! Rich, creamy, and perfectly balanced."),
                (4, "Iced latte is refreshing and not too sweet. Great for afternoon pick-me-up."),
                (5, "Love the McCafe selection. The chocolate muffin pairs perfectly with coffee."),
                (4, "Good coffee quality for a fast food place. The baristas know what they're doing."),
                (3, "Decent coffee but sometimes inconsistent. When it's good, it's really good."),
                (5, "Best coffee value in town! The cappuccino foam art is a nice touch.")
            },
            ["Drinks"] = new()
            {
                (4, "The cola is always perfectly carbonated and ice-cold. Refreshing!"),
                (5, "Iced lemon tea is the perfect balance of sweet and tangy. Love it!"),
                (4, "Orange juice tastes fresh and natural. Great with any meal."),
                (3, "Good drink selection but wish there were more sugar-free options."),
                (5, "Drinks are always served at the perfect temperature. Great quality!")
            },
            ["Snacks"] = new()
            {
                (5, "World Famous Fries live up to their name! Golden, crispy, perfectly salted."),
                (4, "McNuggets are consistently good. Great for sharing or as a snack."),
                (5, "Onion rings are crispy and flavorful. Perfect side dish!"),
                (4, "Great snack options when you want something quick and tasty."),
                (3, "Good snacks but sometimes the fries are a bit too salty for my taste.")
            },
            ["Desserts"] = new()
            {
                (5, "McFlurry with OREO cookies is heavenly! Perfect sweet ending to any meal."),
                (4, "Apple pie is warm and flaky. Reminds me of homemade desserts."),
                (5, "Chocolate sundae is rich and creamy. The fudge topping is amazing!"),
                (4, "Great dessert selection. The soft serve is always the perfect consistency."),
                (3, "Good desserts but wish they had more variety. Still enjoyable though.")
            },
            ["Limited Time"] = new()
            {
                (5, "The Spicy Habanero Burger is incredible! Perfect amount of heat and flavor."),
                (4, "Durian McFlurry is unique and delicious! Love the local twist."),
                (5, "Limited time offers are always exciting. This habanero burger is a winner!"),
                (3, "Interesting flavors but might be too spicy for some. I enjoyed it though."),
                (4, "Love trying the limited time items. They're always creative and tasty!")
            }
        };

        public static async Task SeedReviews(IServiceProvider serviceProvider, bool forceReseed = false)
        {
            try
            {
                var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                Console.WriteLine("Starting review seeding check...");

                // Check if reviews already exist
                var existingReviewCount = await context.Reviews.CountAsync();
                Console.WriteLine($"Found {existingReviewCount} existing reviews.");
                
                if (existingReviewCount > 0 && !forceReseed)
                {
                    Console.WriteLine("Reviews already exist. Skipping review seeding.");
                    return;
                }
                
                if (forceReseed && existingReviewCount > 0)
                {
                    Console.WriteLine("Force reseeding requested. Clearing existing reviews...");
                    var existingReviews = await context.Reviews.ToListAsync();
                    var existingVotes = await context.ReviewVotes.ToListAsync();
                    
                    context.ReviewVotes.RemoveRange(existingVotes);
                    context.Reviews.RemoveRange(existingReviews);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Existing reviews and votes cleared.");
                }

            Console.WriteLine("Starting review seeding process...");

            // Create sample customers
            var createdUsers = new List<ApplicationUser>();
            foreach (var (firstName, lastName, email, userName) in SampleCustomers)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userName,
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = firstName,
                        LastName = lastName,
                        Points = _random.Next(50, 500) // Give them some random points
                    };

                    var result = await userManager.CreateAsync(user, "Customer123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Customer");
                        createdUsers.Add(user);
                        Console.WriteLine($"Created customer: {firstName} {lastName}");
                    }
                }
                else
                {
                    createdUsers.Add(existingUser);
                }
            }

            // Get all menu items
            var menuItems = await context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable)
                .ToListAsync();

            Console.WriteLine($"Found {menuItems.Count} menu items to review.");

            var reviewsToAdd = new List<Review>();
            
            foreach (var menuItem in menuItems)
            {
                // Determine category for review template selection
                var categoryName = GetReviewCategory(menuItem.Category?.Name ?? "");
                var templates = ReviewTemplates.ContainsKey(categoryName) 
                    ? ReviewTemplates[categoryName] 
                    : ReviewTemplates["Burgers"]; // fallback

                // Create 2-5 reviews per item
                var reviewCount = _random.Next(2, 6);
                var usedUsers = new HashSet<string>();

                for (int i = 0; i < reviewCount && usedUsers.Count < createdUsers.Count; i++)
                {
                    // Select a random user who hasn't reviewed this item yet
                    ApplicationUser? selectedUser;
                    do
                    {
                        selectedUser = createdUsers[_random.Next(createdUsers.Count)];
                    }
                    while (usedUsers.Contains(selectedUser.Id) && usedUsers.Count < createdUsers.Count);

                    if (usedUsers.Contains(selectedUser.Id))
                        break; // All users have reviewed this item

                    usedUsers.Add(selectedUser.Id);

                    // Select random review template
                    var template = templates[_random.Next(templates.Count)];
                    
                    // Sometimes make it anonymous (20% chance)
                    var isAnonymous = _random.NextDouble() < 0.2;
                    
                    var review = new Review
                    {
                        UserId = selectedUser.Id,
                        MenuItemId = menuItem.Id,
                        Rating = template.Rating,
                        Comment = template.Comment,
                        IsAnonymous = isAnonymous,
                        AnonymousName = isAnonymous ? GenerateAnonymousName() : null,
                        CreatedDate = DateTime.UtcNow.AddDays(-_random.Next(1, 90)), // Random date within last 90 days
                        IsVerified = true,
                        HelpfulCount = _random.Next(0, 15), // Random helpful votes
                        UnhelpfulCount = _random.Next(0, 5)  // Random unhelpful votes
                    };

                    reviewsToAdd.Add(review);
                }
            }

            // Add all reviews to database
            await context.Reviews.AddRangeAsync(reviewsToAdd);
            await context.SaveChangesAsync();

            Console.WriteLine($"Successfully created {reviewsToAdd.Count} sample reviews!");

            // Update menu item ratings
            Console.WriteLine("Updating menu item ratings...");
            foreach (var menuItem in menuItems)
            {
                var itemReviews = reviewsToAdd.Where(r => r.MenuItemId == menuItem.Id).ToList();
                if (itemReviews.Any())
                {
                    menuItem.AverageRating = (decimal)itemReviews.Average(r => r.Rating);
                    menuItem.TotalReviews = itemReviews.Count;
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine("Menu item ratings updated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during review seeding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static string GetReviewCategory(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                var c when c.Contains("burger") || c.Contains("sandwich") => "Burgers",
                var c when c.Contains("combo") || c.Contains("meal") => "Combos",
                var c when c.Contains("breakfast") => "Breakfast",
                var c when c.Contains("happy") => "Happy Meals",
                var c when c.Contains("coffee") || c.Contains("mccafe") => "Coffee",
                var c when c.Contains("drink") => "Drinks",
                var c when c.Contains("snack") || c.Contains("side") => "Snacks",
                var c when c.Contains("dessert") => "Desserts",
                var c when c.Contains("limited") => "Limited Time",
                _ => "Burgers"
            };
        }

        private static string GenerateAnonymousName()
        {
            var anonymousNames = new[]
            {
                "Anonymous Foodie", "Happy Customer", "Regular Visitor", "Food Lover",
                "Satisfied Customer", "Local Resident", "Frequent Diner", "Anonymous User",
                "Mystery Customer", "Food Enthusiast", "Anonymous Reviewer", "Happy Eater"
            };
            return anonymousNames[_random.Next(anonymousNames.Length)];
        }
    }
}
