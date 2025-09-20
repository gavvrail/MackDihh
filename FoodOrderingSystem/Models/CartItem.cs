using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        // Foreign key to link to the Cart
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        // Foreign key to link to the MenuItem
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        // Properties for points redemption
        public bool IsRedeemedWithPoints { get; set; } = false;
        public int PointsUsed { get; set; } = 0;
        public string? RedemptionCode { get; set; }
    }
}
