using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
        
        [Required]
        [StringLength(200)]
        public string DeliveryAddress { get; set; } = "";
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = "";
        
        [StringLength(500)]
        public string? SpecialInstructions { get; set; }
        
        // Payment Information
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? PaymentStatus { get; set; }
        
        // Delivery Information
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }
        
        // Points and Discounts
        public int PointsEarned { get; set; } = 0;
        public int PointsUsed { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        
        // Cancellation Information
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledBy { get; set; } // "Customer" or "Admin"
        
        // Additional properties that were missing
        [StringLength(50)]
        public string OrderNumber { get; set; } = "";
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Tax { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DeliveryFee { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }
        
        [StringLength(20)]
        public string CustomerPhone { get; set; } = "";
        
        [StringLength(500)]
        public string? DeliveryInstructions { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        public List<OrderItem> OrderItems { get; set; } = new();
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparing,
        Ready,
        ReadyForDelivery,
        Delivering,
        OutForDelivery,
        Delivered,
        Cancelled
    }
}
