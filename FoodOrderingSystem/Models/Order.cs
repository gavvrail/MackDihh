using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Preparing = 2,
        Ready = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DeliveryFee { get; set; } = 0;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? DeliveryAddress { get; set; }
        public string? DeliveryInstructions { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }

        // Foreign key to link the order to a user
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        // An order can have many items
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
