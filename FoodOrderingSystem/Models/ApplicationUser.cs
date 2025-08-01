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
    }
}
