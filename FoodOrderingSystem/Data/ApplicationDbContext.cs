using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Add these two lines for the new cart tables
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Deals and Promotions
        public DbSet<Deal> Deals { get; set; }
        public DbSet<PointsReward> PointsRewards { get; set; }
        public DbSet<UserPointsTransaction> UserPointsTransactions { get; set; }
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; }
        public DbSet<UserRedemption> UserRedemptions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Add unique constraint to prevent duplicate cart items
            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.MenuItemId })
                .IsUnique();

            // Configurations for ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                // Existing unique constraint for referral codes
                entity.HasIndex(u => u.ReferralCode)
                    .IsUnique()
                    .HasFilter("[ReferralCode] IS NOT NULL");

                // Existing unique constraint for student IDs
                entity.HasIndex(u => u.StudentId)
                    .IsUnique()
                    .HasFilter("[StudentId] IS NOT NULL");

                // NEW: Added precision for decimal property
                entity.Property(u => u.ReferralCredits).HasPrecision(18, 2);
            });

            // Configurations for Deal
            builder.Entity<Deal>(entity =>
            {
                // Existing unique constraint for promo codes
                entity.HasIndex(d => d.PromoCode)
                    .IsUnique()
                    .HasFilter("[PromoCode] IS NOT NULL");

                // NEW: Added precision for decimal properties
                entity.Property(d => d.OriginalPrice).HasPrecision(18, 2);
                entity.Property(d => d.DiscountedPrice).HasPrecision(18, 2);
                entity.Property(d => d.MinimumOrderAmount).HasPrecision(18, 2);
                entity.Property(d => d.DiscountPercentage).HasPrecision(5, 2);
            });

            // NEW: Added configuration for MemberSubscription
            builder.Entity<MemberSubscription>(entity =>
            {
                entity.Property(ms => ms.Amount).HasPrecision(18, 2);
            });

            // NEW: Added configuration for PointsReward
            builder.Entity<PointsReward>(entity =>
            {
                entity.Property(pr => pr.DiscountAmount).HasPrecision(18, 2);
                entity.Property(pr => pr.DiscountPercentage).HasPrecision(5, 2);
            });
        }
    }
}