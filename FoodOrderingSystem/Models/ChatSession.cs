using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        
        public string? AgentId { get; set; }
        
        public string? AgentName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastMessageAt { get; set; }
        
        public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;
        
        public string? Subject { get; set; }
        
        public int MessageCount { get; set; } = 0;
        
        public int UnreadCount { get; set; } = 0;
        
        // Navigation properties
        public ApplicationUser Customer { get; set; } = null!;
        public ApplicationUser? Agent { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
    
    public enum ChatSessionStatus
    {
        Active,
        Resolved,
        Closed
    }
}
