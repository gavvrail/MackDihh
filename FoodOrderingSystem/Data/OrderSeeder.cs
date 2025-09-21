using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Data
{
    public static class OrderSeeder
    {
        private static readonly Random _random = new Random();

        public static async Task SeedOrders(IServiceProvider serviceProvider, bool forceReseed = false)
        {
            try
            {
                var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                Console.WriteLine("Starting order seeding check...");

                // Check if orders already exist
                var existingOrderCount = await context.Orders.CountAsync();
                Console.WriteLine($"Found {existingOrderCount} existing orders.");
                
                if (existingOrderCount > 0 && !forceReseed)
                {
                    Console.WriteLine("Orders already exist. Skipping order seeding.");
                    return;
                }
                
                if (forceReseed && existingOrderCount > 0)
                {
                    Console.WriteLine("Force reseeding requested. Clearing existing orders...");
                    var existingOrderItems = await context.OrderItems.ToListAsync();
                    var existingOrders = await context.Orders.ToListAsync();
                    
                    context.OrderItems.RemoveRange(existingOrderItems);
                    context.Orders.RemoveRange(existingOrders);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Existing orders cleared.");
                }

                Console.WriteLine("Starting order seeding process...");

                // Get all users (including sample customers)
                var users = await userManager.Users.Where(u => !u.Email.Contains("admin")).ToListAsync();
                if (!users.Any())
                {
                    Console.WriteLine("No customer users found. Cannot create sample orders.");
                    return;
                }

                // Get all available menu items
                var menuItems = await context.MenuItems
                    .Where(m => m.IsAvailable)
                    .ToListAsync();

                if (!menuItems.Any())
                {
                    Console.WriteLine("No menu items found. Cannot create sample orders.");
                    return;
                }

                var ordersToAdd = new List<Order>();
                var orderItemsToAdd = new List<OrderItem>();
                var orderNumber = 1000;

                // Create orders for the last 6 months
                var startDate = DateTime.UtcNow.AddMonths(-6);
                var endDate = DateTime.UtcNow;

                // Create 50-80 sample orders
                var totalOrders = _random.Next(50, 81);
                Console.WriteLine($"Creating {totalOrders} sample orders...");

                for (int i = 0; i < totalOrders; i++)
                {
                    var randomUser = users[_random.Next(users.Count)];
                    var orderDate = GetRandomDate(startDate, endDate);
                    
                    // Determine order status based on date (older orders more likely to be completed)
                    var daysSinceOrder = (DateTime.UtcNow - orderDate).TotalDays;
                    var status = GetOrderStatus(daysSinceOrder);

                    var order = new Order
                    {
                        OrderNumber = $"ORD{orderNumber++}",
                        UserId = randomUser.Id,
                        OrderDate = orderDate,
                        Status = status,
                        DeliveryAddress = GetRandomAddress(),
                        PhoneNumber = GetRandomPhoneNumber(),
                        PaymentMethod = GetRandomPaymentMethod(),
                        PaymentStatus = status == OrderStatus.Delivered ? "Completed" : "Pending",
                        EstimatedDeliveryTime = orderDate.AddMinutes(_random.Next(30, 120)),
                        ActualDeliveryTime = status == OrderStatus.Delivered ? orderDate.AddMinutes(_random.Next(25, 90)) : null,
                        SpecialInstructions = GetRandomSpecialInstructions()
                    };

                    // Add random items to the order
                    var itemCount = _random.Next(1, 5); // 1-4 items per order
                    decimal orderTotal = 0;
                    int totalPoints = 0;

                    for (int j = 0; j < itemCount; j++)
                    {
                        var randomItem = menuItems[_random.Next(menuItems.Count)];
                        var quantity = _random.Next(1, 4); // 1-3 quantity per item
                        var unitPrice = randomItem.Price;
                        var totalPrice = unitPrice * quantity;

                        var orderItem = new OrderItem
                        {
                            MenuItemId = randomItem.Id,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Price = totalPrice
                        };

                        orderItemsToAdd.Add(orderItem);
                        order.OrderItems.Add(orderItem);
                        
                        orderTotal += totalPrice;
                        totalPoints += randomItem.PointsPerItem * quantity;
                    }

                    // Apply random discount occasionally
                    if (_random.NextDouble() < 0.3) // 30% chance of discount
                    {
                        order.DiscountAmount = Math.Round(orderTotal * 0.1m, 2); // 10% discount
                        orderTotal -= order.DiscountAmount;
                    }

                    // Add delivery fee
                    var deliveryFee = 5.99m;
                    order.DeliveryFee = deliveryFee;
                    orderTotal += deliveryFee;

                    // Set totals
                    order.Subtotal = orderTotal - deliveryFee + order.DiscountAmount;
                    order.Tax = Math.Round(order.Subtotal * 0.06m, 2); // 6% tax
                    order.Total = orderTotal + order.Tax;
                    order.TotalAmount = order.Total;

                    // Points
                    order.PointsEarned = status == OrderStatus.Delivered ? totalPoints : 0;
                    order.PointsUsed = _random.NextDouble() < 0.2 ? _random.Next(50, 200) : 0; // 20% chance of using points

                    ordersToAdd.Add(order);
                }

                // Add all orders to database
                await context.Orders.AddRangeAsync(ordersToAdd);
                await context.SaveChangesAsync();

                Console.WriteLine($"Successfully created {ordersToAdd.Count} sample orders!");
                Console.WriteLine($"Total revenue generated: RM{ordersToAdd.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.Total):F2}");
                
                // Update user points for delivered orders
                Console.WriteLine("Updating user points...");
                var deliveredOrders = ordersToAdd.Where(o => o.Status == OrderStatus.Delivered).ToList();
                foreach (var order in deliveredOrders)
                {
                    var user = await userManager.FindByIdAsync(order.UserId);
                    if (user != null)
                    {
                        user.Points += order.PointsEarned;
                        user.TotalPointsEarned += order.PointsEarned;
                        user.Points -= order.PointsUsed;
                        user.TotalPointsRedeemed += order.PointsUsed;
                        await userManager.UpdateAsync(user);
                    }
                }

                Console.WriteLine("Order seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during order seeding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static DateTime GetRandomDate(DateTime startDate, DateTime endDate)
        {
            var range = endDate - startDate;
            var randomDays = _random.Next(0, range.Days + 1);
            var randomHours = _random.Next(0, 24);
            var randomMinutes = _random.Next(0, 60);
            
            return startDate.AddDays(randomDays).AddHours(randomHours).AddMinutes(randomMinutes);
        }

        private static OrderStatus GetOrderStatus(double daysSinceOrder)
        {
            // Older orders are more likely to be completed
            if (daysSinceOrder > 7) // Orders older than a week
            {
                return _random.NextDouble() switch
                {
                    < 0.85 => OrderStatus.Delivered,
                    < 0.95 => OrderStatus.Cancelled,
                    _ => OrderStatus.Preparing
                };
            }
            else if (daysSinceOrder > 1) // Orders 1-7 days old
            {
                return _random.NextDouble() switch
                {
                    < 0.70 => OrderStatus.Delivered,
                    < 0.80 => OrderStatus.OutForDelivery,
                    < 0.85 => OrderStatus.Preparing,
                    < 0.95 => OrderStatus.Cancelled,
                    _ => OrderStatus.Pending
                };
            }
            else // Recent orders (last day)
            {
                return _random.NextDouble() switch
                {
                    < 0.30 => OrderStatus.Delivered,
                    < 0.50 => OrderStatus.OutForDelivery,
                    < 0.70 => OrderStatus.Preparing,
                    < 0.85 => OrderStatus.Confirmed,
                    _ => OrderStatus.Pending
                };
            }
        }

        private static string GetRandomAddress()
        {
            var addresses = new[]
            {
                "123 Jalan Bukit Bintang, Kuala Lumpur",
                "456 Lorong Maarof, Bangsar, Kuala Lumpur",
                "789 Jalan Telawi, Bangsar Baru, KL",
                "321 Jalan Sultan Ismail, KLCC, Kuala Lumpur",
                "654 Jalan Ampang, Ampang, Selangor",
                "987 Jalan PJU 7/3, Mutiara Damansara, Selangor",
                "147 Jalan SS 2/24, Petaling Jaya, Selangor",
                "258 Jalan Gasing, Petaling Jaya, Selangor",
                "369 Persiaran Surian, Kota Damansara, Selangor",
                "741 Jalan 14/20, Petaling Jaya, Selangor"
            };
            return addresses[_random.Next(addresses.Length)];
        }

        private static string GetRandomPhoneNumber()
        {
            return $"01{_random.Next(1, 9)}-{_random.Next(100, 999)}{_random.Next(1000, 9999)}";
        }

        private static string GetRandomPaymentMethod()
        {
            var methods = new[] { "Card", "Cash", "Online Banking", "E-Wallet" };
            return methods[_random.Next(methods.Length)];
        }

        private static string? GetRandomSpecialInstructions()
        {
            if (_random.NextDouble() < 0.4) // 40% chance of having special instructions
            {
                var instructions = new[]
                {
                    "Please call when you arrive",
                    "Leave at the door",
                    "Ring the bell twice",
                    "No onions please",
                    "Extra spicy",
                    "Please include extra sauce",
                    "Contactless delivery preferred",
                    "Call 5 minutes before arrival",
                    "Leave with security guard",
                    "Apartment unit 12-3A"
                };
                return instructions[_random.Next(instructions.Length)];
            }
            return null;
        }
    }
}
