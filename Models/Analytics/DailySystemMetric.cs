using System.ComponentModel.DataAnnotations;

namespace Ping.Models.Analytics
{
    public class DailySystemMetric
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        [Required]
        public required string MetricType { get; set; } // "DAU", "WAU", "MAU", "NewUsers", etc.

        public double Value { get; set; }

        /// <summary>
        /// JSON string storing breakdown (e.g., by region, platform, etc.)
        /// </summary>
        public string? Dimensions { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

