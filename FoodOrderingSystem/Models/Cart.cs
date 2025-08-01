using System.Collections.Generic;

namespace FoodOrderingSystem.Models
{
    public class Cart
    {
        public int Id { get; set; }

        // Each cart belongs to a specific user
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        // A cart can have many items
        public List<CartItem> CartItems { get; set; } = new();
    }
}
