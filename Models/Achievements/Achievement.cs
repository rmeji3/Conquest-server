using System.ComponentModel.DataAnnotations;
using Ping.Models.Stickers;

namespace Ping.Models.Achievements;

/// <summary>Which user activity an achievement measures. Extension point for new
/// achievement kinds — add a member here plus a count query in AchievementService.</summary>
public enum AchievementMetric
{
    ReviewsCreated = 0,
    PingsCreated = 1,
    Followers = 2,
    /// <summary>Reviews that include a specific tag — see <see cref="Achievement.FilterTag"/>.</summary>
    ReviewsWithTag = 3,
}

/// <summary>
/// An admin-defined milestone: reach <see cref="Threshold"/> on <see cref="Metric"/>
/// to automatically earn <see cref="RewardSticker"/>. Unlocks are recorded as
/// <see cref="UserAchievement"/> rows.
/// </summary>
public class Achievement
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Stable, unique identifier (e.g. "reviews_25"). Useful for clients and analytics.</summary>
    [Required]
    [MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    [Required]
    public AchievementMetric Metric { get; set; }

    /// <summary>
    /// Required when <see cref="Metric"/> is <see cref="AchievementMetric.ReviewsWithTag"/>.
    /// Normalized lowercase review tag name (e.g. "matcha", "coffee").
    /// </summary>
    [MaxLength(64)]
    public string? FilterTag { get; set; }

    /// <summary>Metric count required to unlock. Must be at least 1.</summary>
    public int Threshold { get; set; }

    [Required]
    public string RewardStickerId { get; set; } = string.Empty;

    public Sticker? RewardSticker { get; set; }

    /// <summary>Inactive achievements are hidden from users and never unlock.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display order in the achievements list (ascending).</summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
