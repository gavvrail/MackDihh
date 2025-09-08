using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review cannot be longer than 1000 characters")]
        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public bool IsVerified { get; set; } = false;

        public bool IsHelpful { get; set; } = false;

        public int HelpfulCount { get; set; } = 0;

        public int UnhelpfulCount { get; set; } = 0;

        public bool IsAnonymous { get; set; } = false;

        [StringLength(200)]
        public string? AnonymousName { get; set; }

        // Review images
        public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();

        // Review responses
        public virtual ICollection<ReviewResponse> Responses { get; set; } = new List<ReviewResponse>();
    }

    public class ReviewImage
    {
        public int Id { get; set; }

        [Required]
        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Caption { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ReviewResponse
    {
        public int Id { get; set; }

        [Required]
        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;

        [Required]
        public string ResponderId { get; set; } = string.Empty;
        public ApplicationUser Responder { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Response { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsFromBusiness { get; set; } = false;
    }
}
