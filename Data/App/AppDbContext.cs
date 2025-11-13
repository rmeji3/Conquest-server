using Conquest.Models.Activities;
using Conquest.Models.Places;
using Microsoft.EntityFrameworkCore;

namespace Conquest.Data.App
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<Place> Places => Set<Place>();
        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<Place>()
              .HasIndex(p => new { p.Latitude, p.Longitude }); // basic geo index

            mb.Entity<Activity>()
              .HasIndex(a => new { a.PlaceId, a.Type });
        }
    }
}
