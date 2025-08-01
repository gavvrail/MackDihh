using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class MapConfiguration
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Location Name")]
        public string LocationName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Latitude")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }
        
        [Required]
        [Display(Name = "Longitude")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }
        
        [Display(Name = "Zoom Level")]
        [Range(1, 20, ErrorMessage = "Zoom level must be between 1 and 20")]
        public int ZoomLevel { get; set; } = 15;
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
} 