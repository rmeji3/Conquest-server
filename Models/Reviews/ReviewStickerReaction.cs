using System.ComponentModel.DataAnnotations;
using Ping.Models.Stickers;

namespace Ping.Models.Reviews;

/// <summary>
/// One unique sticker reaction by a user on a review. Users can choose up to 10
/// different stickers per review (cap enforced in ReviewService) and can only use
/// stickers they own (<see cref="UserSticker"/>).
/// </summary>
public class ReviewStickerReaction
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string StickerId { get; set; } = string.Empty;
    public Sticker? Sticker { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
