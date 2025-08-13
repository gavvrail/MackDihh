using Microsoft.AspNetCore.Identity;
using System;

namespace FoodOrderingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public DateTime? LastUsernameChangeDate { get; set; }
        public int UsernameChangeCount { get; set; } = 0;
        
        // Member and Points System
        public bool IsMember { get; set; } = false;
        public DateTime? MemberExpiryDate { get; set; }
        public int Points { get; set; } = 0;
        public int TotalPointsEarned { get; set; } = 0;
        public int TotalPointsRedeemed { get; set; } = 0;
        
        // Student Verification
        public bool IsStudentVerified { get; set; } = false;
        public string? StudentId { get; set; }
        public string? InstitutionName { get; set; }
        public DateTime? StudentVerificationDate { get; set; }
        
        // Referral System
        public string? ReferralCode { get; set; }
        public string? ReferredBy { get; set; }
        public int ReferralCount { get; set; } = 0;
        public decimal ReferralCredits { get; set; } = 0;
        
        // Member Purchase History
        public DateTime? LastMemberPurchaseDate { get; set; }
        public int MemberPurchaseCount { get; set; } = 0;
        
        // User Creation Date
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
