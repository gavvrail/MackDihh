using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int SessionId { get; set; }
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        
        [Required]
        public string SenderName { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsFromCustomer { get; set; } = true;
        
        public bool IsRead { get; set; } = false;
        
        // Navigation properties
        public ApplicationUser Sender { get; set; } = null!;
        public ChatSession Session { get; set; } = null!;
    }
}
