using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services
{
    public class OrderStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderStatusUpdateService> _logger;

        public OrderStatusUpdateService(IServiceProvider serviceProvider, ILogger<OrderStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateOrderStatuses();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating order statuses");
                }

                // Check every 2 minutes
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        private async Task UpdateOrderStatuses()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var ordersToUpdate = await context.Orders
                .Where(o => o.Status != OrderStatus.Delivered && 
                           o.Status != OrderStatus.Cancelled &&
                           o.EstimatedDeliveryTime.HasValue)
                .ToListAsync();

            foreach (var order in ordersToUpdate)
            {
                var timeElapsed = now - order.OrderDate;
                var estimatedDeliveryTime = order.EstimatedDeliveryTime!.Value;
                var totalDeliveryTime = estimatedDeliveryTime - order.OrderDate;

                // Calculate status based on time progression
                var progressPercentage = timeElapsed.TotalMinutes / totalDeliveryTime.TotalMinutes;

                OrderStatus newStatus = order.Status;

                // Automatic status progression based on time
                if (progressPercentage >= 1.0 && order.Status < OrderStatus.Delivered)
                {
                    newStatus = OrderStatus.Delivered;
                    order.ActualDeliveryTime = now;
                }
                else if (progressPercentage >= 0.8 && order.Status < OrderStatus.OutForDelivery)
                {
                    newStatus = OrderStatus.OutForDelivery;
                }
                else if (progressPercentage >= 0.6 && order.Status < OrderStatus.Ready)
                {
                    newStatus = OrderStatus.Ready;
                }
                else if (progressPercentage >= 0.3 && order.Status < OrderStatus.Preparing)
                {
                    newStatus = OrderStatus.Preparing;
                }
                else if (progressPercentage >= 0.1 && order.Status < OrderStatus.Confirmed)
                {
                    newStatus = OrderStatus.Confirmed;
                }

                if (newStatus != order.Status)
                {
                    order.Status = newStatus;
                    _logger.LogInformation("Updated order {OrderNumber} status from {OldStatus} to {NewStatus}", 
                        order.OrderNumber, order.Status, newStatus);
                }
            }

            if (ordersToUpdate.Any(o => context.Entry(o).State == EntityState.Modified))
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} order statuses", 
                    ordersToUpdate.Count(o => context.Entry(o).State == EntityState.Modified));
            }
        }
    }
}
