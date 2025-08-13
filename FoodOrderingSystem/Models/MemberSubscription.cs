using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class MemberSubscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        
        public MemberPlan Plan { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        
        public string? TransactionId { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    
    public enum MemberPlan
    {
        Monthly = 1,
        Quarterly = 3,
        Yearly = 12
    }
    
    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        Pending
    }
} 