using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class MenuItemImage
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        [StringLength(100)]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? ImageType { get; set; } // "main", "thumbnail", "gallery", etc.

        public long FileSize { get; set; } = 0;

        [StringLength(20)]
        public string? FileExtension { get; set; }
    }
}
