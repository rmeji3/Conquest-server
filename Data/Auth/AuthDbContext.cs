using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Conquest.Models.AppUsers;
using Conquest.Models.Friends;
using Conquest.Models.Users; // Added for UserBlock

namespace Conquest.Data.Auth
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<AppUser>(options)
    {
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
        public DbSet<IpBan> IpBans => Set<IpBan>();
        public DbSet<Conquest.Models.Analytics.UserActivityLog> UserActivityLogs => Set<Conquest.Models.Analytics.UserActivityLog>();
        public DbSet<Conquest.Models.Analytics.DailySystemMetric> DailySystemMetrics => Set<Conquest.Models.Analytics.DailySystemMetric>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Friendship
            builder.Entity<Friendship>()
                .HasKey(f => new { f.UserId, f.FriendId });

            builder.Entity<Friendship>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(f => f.Friend)
                .WithMany()
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            // AppUser - usually Identity handles this but maintaining existing index
            builder.Entity<AppUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            // UserBlock
            builder.Entity<UserBlock>()
                .HasKey(ub => new { ub.BlockerId, ub.BlockedId });

            builder.Entity<UserBlock>()
                .HasOne(ub => ub.Blocker)
                .WithMany()
                .HasForeignKey(ub => ub.BlockerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBlock>()
                .HasOne(ub => ub.Blocked)
                .WithMany()
                .HasForeignKey(ub => ub.BlockedId)
                .OnDelete(DeleteBehavior.Cascade);

            // Analytics
            builder.Entity<Conquest.Models.Analytics.UserActivityLog>()
                .HasIndex(l => new { l.UserId, l.Date })
                .IsUnique(); // One log per user per day

             builder.Entity<Conquest.Models.Analytics.UserActivityLog>()
                .HasIndex(l => l.Date); // Optimize aggregation by date

            builder.Entity<Conquest.Models.Analytics.DailySystemMetric>()
                .HasIndex(m => m.Date);
        }
    }
}
