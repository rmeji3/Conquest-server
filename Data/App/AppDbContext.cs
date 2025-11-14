using Conquest.Models.Activities;
using Conquest.Models.Places;
using Microsoft.EntityFrameworkCore;
using Conquest.Models.Friends;

namespace Conquest.Data.App
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }
        public DbSet<Place> Places => Set<Place>();
        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Place>()
              .HasIndex(p => new { p.Latitude, p.Longitude }); // basic geo index

            builder.Entity<Activity>()
              .HasIndex(a => new { a.PlaceId, a.Type });
        }
    }
}
