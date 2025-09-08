using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class WishListItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int Priority { get; set; } = 1; // 1 = Low, 2 = Medium, 3 = High

        public bool IsPublic { get; set; } = false;

        [StringLength(100)]
        public string? WishListName { get; set; } = "My Wishlist";
    }
}
