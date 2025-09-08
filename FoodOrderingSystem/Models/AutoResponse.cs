using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class AutoResponse
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Keywords { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Response { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? CreatedBy { get; set; }
        
        public string? UpdatedBy { get; set; }
        
        // Navigation properties
        public ApplicationUser? Creator { get; set; }
        public ApplicationUser? Updater { get; set; }
    }
}
