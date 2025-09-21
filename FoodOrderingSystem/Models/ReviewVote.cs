using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class ReviewVote
    {
        public int Id { get; set; }
        
        [Required]
        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        [Required]
        public VoteType VoteType { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
    
    public enum VoteType
    {
        Helpful = 1,
        Unhelpful = 2
    }
}
