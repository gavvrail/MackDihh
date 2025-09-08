using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class CroppedImageModel
    {
        [Required]
        public string ImageData { get; set; } = string.Empty;
        
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
    }
}
