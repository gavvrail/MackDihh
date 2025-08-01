using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 1000.00, ErrorMessage = "Price must be between 0.01 and 1000.00")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Display(Name = "Image URL")]
        // This is the corrected attribute. It allows local paths like /images/my-image.png
        [DataType(DataType.Text)]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Preparation Time (minutes)")]
        [Range(1, 120, ErrorMessage = "Preparation time must be between 1 and 120 minutes")]
        public int PreparationTimeMinutes { get; set; } = 15;

        [Display(Name = "Calories")]
        [Range(0, 5000, ErrorMessage = "Calories must be between 0 and 5000")]
        public int? Calories { get; set; }

        [Display(Name = "Allergens")]
        public string? Allergens { get; set; }
    }
}
