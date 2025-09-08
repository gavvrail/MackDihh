using System.Collections.Generic;

namespace FoodOrderingSystem.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public string? ProfilePhotoUrl { get; set; }
        public int Points { get; set; }
        
        // Blocking properties
        public bool IsBlocked { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
        public int LoginAttempts { get; set; }
        public DateTime? LastLoginAttempt { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
