using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class OrderCancellation
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        
        [Required]
        public CancellationReasonType ReasonType { get; set; }
        
        [StringLength(500)]
        public string? AdditionalDetails { get; set; }
        
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        
        public bool IsReviewedByAdmin { get; set; } = false;
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
    
    public enum CancellationReasonType
    {
        WrongDeliveryAddress,
        WantToAddMoreItems,
        ChangedMind,
        FoundBetterDeal,
        DeliveryTimeTooLong,
        PaymentIssues,
        ItemOutOfStock,
        Emergency,
        Other
    }
}
