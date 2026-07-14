using Ping.Dtos.Achievements;
using Ping.Models.Achievements;

namespace Ping.Services.Achievements;

public interface IAchievementService
{
    /// <summary>
    /// Checks every active achievement on <paramref name="metric"/> the user hasn't
    /// unlocked yet and, for each met threshold, records the unlock, grants the reward
    /// sticker, and notifies the user. Idempotent and race-safe — call freely after
    /// the underlying activity is saved.
    /// </summary>
    Task CheckAndUnlockAsync(string userId, AchievementMetric metric);

    /// <summary>
    /// Returns all active achievements with the user's progress. Reconciles first
    /// (same checks as <see cref="CheckAndUnlockAsync"/> across all metrics), so
    /// achievements added after the activity happened still unlock retroactively.
    /// </summary>
    Task<List<AchievementProgressDto>> GetMyAchievementsAsync(string userId);

    // ---- Admin CRUD ----
    Task<AdminAchievementDto> CreateAsync(UpsertAchievementDto dto);
    Task<AdminAchievementDto> UpdateAsync(string id, UpsertAchievementDto dto);
    Task<AdminAchievementDto> ToggleActiveAsync(string id);
    Task DeleteAsync(string id);
    Task<List<AdminAchievementDto>> GetAllForAdminAsync();
}
