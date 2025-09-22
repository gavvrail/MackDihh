using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        // PhoneNumber is inherited from IdentityUser, no need to override

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200)]
        public string? ProfilePhotoUrl { get; set; }

        public DateTime? LastUsernameChangeDate { get; set; }

        public int UsernameChangeCount { get; set; } = 0;

        // New fields for missing features
        public bool IsBlocked { get; set; } = false;
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
        public int LoginAttempts { get; set; } = 0;
        public DateTime? LastLoginAttempt { get; set; }
        public DateTime? LastLoginDate { get; set; }

        // Points system - consolidated into single property
        public int Points { get; set; } = 0;
        public int TotalPointsEarned { get; set; } = 0;
        public int TotalPointsRedeemed { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Referral system
        [StringLength(20)]
        public string? ReferralCode { get; set; }
        public int ReferralPointsEarned { get; set; } = 0;
    }
}
