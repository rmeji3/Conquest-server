using System.ComponentModel.DataAnnotations;
using Conquest.Models.Activities;
using Conquest.Models.Reviews;

namespace Conquest.Models.Places
{
    public class Place
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public required string Name { get; set; } = null!;
        [MaxLength(300)]
        public string? Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [MaxLength(100)]
        public string OwnerUserId { get; set; } = null!;
        public PlaceVisibility Visibility { get; set; } = PlaceVisibility.Private;
        public PlaceType Type { get; set; }
        
        public bool IsClaimed { get; set; }

        public ICollection<PlaceActivity> PlaceActivities { get; set; } = new List<PlaceActivity>();
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public int Favorites { get; set; } = 0;
        public ICollection<Favorited> FavoritedList { get; set; } = [];
    }
}
