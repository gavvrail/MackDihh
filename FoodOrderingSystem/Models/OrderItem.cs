using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; } // Price at the time of purchase
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; } // Unit price at the time of purchase

        // Foreign key to link to the Order
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        // Foreign key to link to the MenuItem
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;
    }
}
