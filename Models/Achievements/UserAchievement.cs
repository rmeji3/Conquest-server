using System.ComponentModel.DataAnnotations;

namespace Ping.Models.Achievements;

/// <summary>
/// Unlock record: this user hit the achievement's threshold and was granted its
/// reward sticker. Unique per (UserId, AchievementId).
/// </summary>
public class UserAchievement
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string AchievementId { get; set; } = string.Empty;

    public Achievement? Achievement { get; set; }

    public DateTimeOffset UnlockedUtc { get; set; } = DateTimeOffset.UtcNow;
}
