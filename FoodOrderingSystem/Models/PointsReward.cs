using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class PointsReward
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";
        
        [StringLength(500)]
        public string Description { get; set; } = "";
        
        public int PointsRequired { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        
        [StringLength(200)]
        public string? ImageUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        public int MaxRedemptions { get; set; } = -1; // -1 means unlimited
        public int CurrentRedemptions { get; set; } = 0;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class UserPointsTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        
        public int Points { get; set; }
        public PointsTransactionType Type { get; set; }
        public string Description { get; set; } = "";
        
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        
        public int? DealId { get; set; }
        public Deal? Deal { get; set; }
        
        public int? PointsRewardId { get; set; }
        public PointsReward? PointsReward { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum PointsTransactionType
    {
        Earned,
        Redeemed,
        Expired,
        Bonus,
        Referral,
        Refunded
    }
    
    public class UserRedemption
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        
        public int? PointsRewardId { get; set; }
        public PointsReward? PointsReward { get; set; }
        
        public int? MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }
        
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
        
        public string? RedemptionCode { get; set; }
    }
} 