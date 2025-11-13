using Conquest.Models.Activities;

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
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
