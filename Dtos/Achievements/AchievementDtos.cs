using System.ComponentModel.DataAnnotations;
using Ping.Models.Achievements;

namespace Ping.Dtos.Achievements;

/// <summary>An achievement with the current user's progress toward it.
/// <paramref name="Current"/> is clamped to <paramref name="Threshold"/> once unlocked.</summary>
public record AchievementProgressDto(
    string Id,
    string Key,
    string Name,
    string? Description,
    AchievementMetric Metric,
    string? FilterTag,
    int Threshold,
    int Current,
    DateTimeOffset? UnlockedUtc,
    string RewardStickerId,
    string RewardStickerKey,
    string RewardStickerName,
    string? RewardStickerImageUrl
);

/// <summary>Full achievement record for the admin dashboard.</summary>
public record AdminAchievementDto(
    string Id,
    string Key,
    string Name,
    string? Description,
    AchievementMetric Metric,
    string? FilterTag,
    int Threshold,
    string RewardStickerId,
    string RewardStickerName,
    string? RewardStickerImageUrl,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedUtc,
    int UnlockCount
);

/// <summary>Create/update payload for admin CRUD. On update, Key stays immutable.</summary>
public record UpsertAchievementDto(
    [Required] string Key,
    [Required] string Name,
    string? Description,
    [Required] AchievementMetric Metric,
    string? FilterTag,
    [Range(1, int.MaxValue)] int Threshold,
    [Required] string RewardStickerId,
    int SortOrder = 0
);
