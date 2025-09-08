using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Data
{
    public static class AutoResponseSeeder
    {
        public static async Task SeedAutoResponsesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Check if auto-responses already exist
            if (context.AutoResponses.Any())
            {
                return;
            }

            // Get the first admin user to set as creator
            var adminUser = await userManager.GetUsersInRoleAsync("Admin");
            var creatorId = adminUser.FirstOrDefault()?.Id;

            var defaultAutoResponses = new List<AutoResponse>
            {
                new AutoResponse
                {
                    Name = "Greeting Response",
                    Keywords = "hello, hi, hey, good morning, good afternoon, good evening",
                    Response = "Hello! Thank you for contacting MackDihh support. How can I assist you today?",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                },
                new AutoResponse
                {
                    Name = "Thank You Response",
                    Keywords = "thank you, thanks, appreciate",
                    Response = "You're welcome! Is there anything else I can help you with?",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                },
                new AutoResponse
                {
                    Name = "Order Status Inquiry",
                    Keywords = "order status, where is my order, track order",
                    Response = "I can help you check your order status. Please provide your order number and I'll look it up for you.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                },
                new AutoResponse
                {
                    Name = "Delivery Time Inquiry",
                    Keywords = "delivery time, when will it arrive, delivery",
                    Response = "Our standard delivery time is 30-45 minutes. For specific delivery inquiries, please provide your order number.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                },
                new AutoResponse
                {
                    Name = "Menu Inquiry",
                    Keywords = "menu, food, what do you have",
                    Response = "You can view our full menu by clicking on the 'Menu' tab. We have a variety of delicious options available!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                },
                new AutoResponse
                {
                    Name = "Operating Hours",
                    Keywords = "hours, open, close, operating hours",
                    Response = "We are open daily from 10:00 AM to 10:00 PM. We're here to serve you!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = creatorId
                }
            };

            context.AutoResponses.AddRange(defaultAutoResponses);
            await context.SaveChangesAsync();
        }
    }
}
