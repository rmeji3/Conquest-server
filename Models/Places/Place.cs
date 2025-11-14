using Conquest.Models.Activities;
using Conquest.Models.Reviews;

namespace Conquest.Models.Places
{
    public class Place
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
