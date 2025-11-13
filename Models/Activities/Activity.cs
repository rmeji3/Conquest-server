namespace Conquest.Models.Activities
{
    using Conquest.Models.Places;
    public class Activity
    {
        public int Id { get; set; }
        public int PlaceId { get; set; }
        public Place Place { get; set; } = null!;
        public string Type { get; set; } = null!; // e.g. "rock_climbing", "tennis", "fishing"
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
