using System.ComponentModel.DataAnnotations;
using Conquest.Models.Places;

namespace Conquest.Models.Business
{
    public class PlaceClaim
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public required string UserId { get; set; } // Logical FK to AuthDbContext.AppUser

        public int PlaceId { get; set; }
        public Place Place { get; set; } = null!;

        [MaxLength(500)]
        public string Proof { get; set; } = string.Empty; // URL or description

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        public DateTime? ReviewedUtc { get; set; }
        
        [MaxLength(100)]
        public string? ReviewerId { get; set; } // Admin who reviewed
    }

    public enum ClaimStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
