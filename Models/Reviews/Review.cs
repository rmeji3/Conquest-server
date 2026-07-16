using System.ComponentModel.DataAnnotations;
using Ping.Models.Pings;

namespace Ping.Models.Reviews;

public class Review
{
    public int Id { get; init; }
    [MaxLength(200)]
    public string UserId { get; init; } = null!;  // FK reference only
    [MaxLength(100)]
    public string UserName { get; init; } = null!;
    // Client-supplied idempotency key. The app's background upload queue sends
    // the same key on every retry of one submission, so a create whose response
    // was lost (app killed mid-request) can't produce a duplicate review.
    // Null for legacy clients that don't send one.
    [MaxLength(64)]
    public string? ClientRequestId { get; init; }
    public int PingActivityId { get; set; }
    public PingActivity PingActivity { get; set; } = null!;
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }
    public ReviewType Type { get; set; }
    [MaxLength(1000)]
    public string? Content { get; set; }
    public DateTime CreatedAt { get; init; }
    public List<ReviewTag> ReviewTags { get; set; } = new();
    public int Likes { get; set; }
    public List<ReviewLike> LikesList { get; set; } = new();
    
    [Required] // Enforce non-null in DB
    [MaxLength(500)]
    public string ImageUrl { get; set; } = null!;
    [Required] // Enforce non-null in DB
    [MaxLength(500)]
    public string ThumbnailUrl { get; set; } = null!;
    
    public List<string> AdditionalImageUrls { get; set; } = new();
}

