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
        public DbSet<UserPromoCode> UserPromoCodes { get; set; }

        // New models for missing features
        public DbSet<MenuItemImage> MenuItemImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ReviewResponse> ReviewResponses { get; set; }
        public DbSet<WishListItem> WishListItems { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        
        // Auto Response System
        public DbSet<AutoResponse> AutoResponses { get; set; }
        
        // Order Cancellation
        public DbSet<OrderCancellation> OrderCancellations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships and constraints for new models
            builder.Entity<MenuItemImage>()
                .HasOne(mi => mi.MenuItem)
                .WithMany(m => m.Images)
                .HasForeignKey(mi => mi.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.MenuItem)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewImage>()
                .HasOne(ri => ri.Review)
                .WithMany(r => r.Images)
                .HasForeignKey(ri => ri.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewResponse>()
                .HasOne(rr => rr.Review)
                .WithMany(r => r.Responses)
                .HasForeignKey(rr => rr.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewResponse>()
                .HasOne(rr => rr.Responder)
                .WithMany()
                .HasForeignKey(rr => rr.ResponderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WishListItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishListItem>()
                .HasOne(w => w.MenuItem)
                .WithMany(m => m.WishListItems)
                .HasForeignKey(w => w.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasOne(cm => cm.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(cm => cm.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatSession>()
                .HasOne(cs => cs.Customer)
                .WithMany()
                .HasForeignKey(cs => cs.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatSession>()
                .HasOne(cs => cs.Agent)
                .WithMany()
                .HasForeignKey(cs => cs.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for better performance
            builder.Entity<Review>()
                .HasIndex(r => new { r.MenuItemId, r.CreatedDate });

            builder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.CreatedDate });

            builder.Entity<WishListItem>()
                .HasIndex(w => new { w.UserId, w.AddedDate });

            builder.Entity<ChatMessage>()
                .HasIndex(cm => new { cm.SenderId, cm.Timestamp });

            builder.Entity<ChatMessage>()
                .HasIndex(cm => new { cm.SessionId, cm.Timestamp });



            builder.Entity<ChatSession>()
                .HasIndex(cs => new { cs.CustomerId, cs.Status });

            // Add unique constraint to prevent duplicate cart items
            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.MenuItemId })
                .IsUnique();

            // Configurations for MenuItem
            builder.Entity<MenuItem>(entity =>
            {
                entity.Property(m => m.Price).HasPrecision(18, 2);
                entity.Property(m => m.AverageRating).HasPrecision(3, 2); // e.g., 4.50
            });

            // Configurations for Order
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
                entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            });

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

            // NEW: Added configuration for UserPromoCode to fix decimal precision warnings
            builder.Entity<UserPromoCode>(entity =>
            {
                entity.Property(upc => upc.DiscountPercentage).HasPrecision(5, 2);
                entity.Property(upc => upc.DiscountedPrice).HasPrecision(18, 2);
                entity.Property(upc => upc.MinimumOrderAmount).HasPrecision(18, 2);
            });

            // NEW: Added configuration for OrderCancellation to fix cascade delete issue
            builder.Entity<OrderCancellation>(entity =>
            {
                entity.HasOne(oc => oc.Order)
                    .WithMany()
                    .HasForeignKey(oc => oc.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(oc => oc.User)
                    .WithMany()
                    .HasForeignKey(oc => oc.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // NEW: Added configuration for AutoResponse
            builder.Entity<AutoResponse>(entity =>
            {
                entity.HasOne(ar => ar.Creator)
                    .WithMany()
                    .HasForeignKey(ar => ar.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ar => ar.Updater)
                    .WithMany()
                    .HasForeignKey(ar => ar.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}