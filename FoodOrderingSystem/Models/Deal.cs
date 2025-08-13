using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Deal
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";
        
        [StringLength(500)]
        public string Description { get; set; } = "";
        
        [Required]
        public DealType Type { get; set; }
        
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        
        [StringLength(50)]
        public string? PromoCode { get; set; }
        
        public int MaxUses { get; set; } = -1; // -1 means unlimited
        public int CurrentUses { get; set; } = 0;
        
        public bool RequiresMember { get; set; } = false;
        public bool RequiresStudentVerification { get; set; } = false;
        public bool IsFlashSale { get; set; } = false;
        public bool IsSeasonal { get; set; } = false;
        
        [StringLength(200)]
        public string? ImageUrl { get; set; }
        
        [StringLength(100)]
        public string? BadgeText { get; set; }
        public string? BadgeColor { get; set; } = "primary";
        
        public int PointsReward { get; set; } = 0;
        public decimal MinimumOrderAmount { get; set; } = 0;
        
        [StringLength(1000)]
        public string? TermsAndConditions { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    
    public enum DealType
    {
        FlashSale,
        BundleOffer,
        SeasonalDiscount,
        PromoCode,
        LimitedTimeOffer,
        MemberDeal,
        StudentDiscount,
        ReferralBonus,
        PointsReward
    }
} 