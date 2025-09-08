using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class UserPromoCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int DealId { get; set; }

        [Required]
        [StringLength(50)]
        public string PromoCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public decimal DiscountPercentage { get; set; } = 0;

        public decimal DiscountedPrice { get; set; } = 0;

        public decimal MinimumOrderAmount { get; set; } = 0;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int MaxUses { get; set; } = 1;

        public int CurrentUses { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }

        public bool IsUsed { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("DealId")]
        public virtual Deal Deal { get; set; } = null!;
    }
}
