using Microsoft.EntityFrameworkCore;
using Ping.Data.App;
using Ping.Data.Auth;
using Ping.Dtos.Achievements;
using Ping.Models.Achievements;
using Ping.Models.Notifications;
using Ping.Models.Stickers;
using Ping.Services.Notifications;

namespace Ping.Services.Achievements;

public class AchievementService(
    AppDbContext appDb,
    AuthDbContext authDb,
    INotificationService notificationService,
    ILogger<AchievementService> logger) : IAchievementService
{
    public async Task CheckAndUnlockAsync(string userId, AchievementMetric metric)
    {
        var candidates = await GetLockedCandidatesAsync(userId, [metric]);
        if (candidates.Count == 0) return;

        await UnlockMetThresholdsAsync(userId, candidates, await BuildCurrentCountsAsync(userId, candidates));
    }

    public async Task<List<AchievementProgressDto>> GetMyAchievementsAsync(string userId)
    {
        var achievements = await appDb.Achievements
            .AsNoTracking()
            .Include(a => a.RewardSticker)
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Threshold)
            .ToListAsync();

        if (achievements.Count == 0) return [];

        var currentByAchievementId = await BuildCurrentCountsAsync(userId, achievements);

        // Lazy reconcile: achievements created after the user's activity happened
        // (or unlocks missed by the write-time hook) are granted retroactively here.
        var candidates = await GetLockedCandidatesAsync(userId, achievements.Select(a => a.Metric).Distinct().ToList());
        if (candidates.Count > 0)
        {
            await UnlockMetThresholdsAsync(userId, candidates, currentByAchievementId);
        }

        var unlockedByAchievementId = await appDb.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.UnlockedUtc);

        return achievements.Select(a =>
        {
            var unlocked = unlockedByAchievementId.TryGetValue(a.Id, out var unlockedUtc);
            var current = currentByAchievementId.GetValueOrDefault(a.Id);
            return new AchievementProgressDto(
                a.Id,
                a.Key,
                a.Name,
                a.Description,
                a.Metric,
                a.FilterTag,
                a.Threshold,
                unlocked ? Math.Max(current, a.Threshold) : current,
                unlocked ? unlockedUtc : null,
                a.RewardStickerId,
                a.RewardSticker!.Key,
                a.RewardSticker.Name,
                a.RewardSticker.ImageUrl);
        }).ToList();
    }

    /// <summary>Active achievements on the given metrics the user hasn't unlocked yet.</summary>
    private async Task<List<Achievement>> GetLockedCandidatesAsync(string userId, List<AchievementMetric> metrics)
    {
        return await appDb.Achievements
            .AsNoTracking()
            .Include(a => a.RewardSticker)
            .Where(a => a.IsActive && metrics.Contains(a.Metric))
            .Where(a => !appDb.UserAchievements.Any(ua => ua.UserId == userId && ua.AchievementId == a.Id))
            .ToListAsync();
    }

    private async Task<Dictionary<string, int>> BuildCurrentCountsAsync(string userId, IReadOnlyList<Achievement> achievements)
    {
        var counts = new Dictionary<string, int>();
        if (achievements.Count == 0) return counts;

        var simpleMetrics = achievements
            .Select(a => a.Metric)
            .Where(m => m != AchievementMetric.ReviewsWithTag)
            .Distinct()
            .ToList();

        var countsByMetric = new Dictionary<AchievementMetric, int>();
        foreach (var metric in simpleMetrics)
        {
            countsByMetric[metric] = await CountMetricAsync(userId, metric);
        }

        var tagFilters = achievements
            .Where(a => a.Metric == AchievementMetric.ReviewsWithTag && !string.IsNullOrEmpty(a.FilterTag))
            .Select(a => a.FilterTag!)
            .Distinct()
            .ToList();
        var countsByTag = await CountReviewsWithTagsAsync(userId, tagFilters);

        foreach (var achievement in achievements)
        {
            counts[achievement.Id] = achievement.Metric switch
            {
                AchievementMetric.ReviewsWithTag => countsByTag.GetValueOrDefault(achievement.FilterTag!, 0),
                _ => countsByMetric.GetValueOrDefault(achievement.Metric, 0),
            };
        }

        return counts;
    }

    private async Task UnlockMetThresholdsAsync(
        string userId,
        List<Achievement> candidates,
        IReadOnlyDictionary<string, int> currentByAchievementId)
    {
        foreach (var achievement in candidates)
        {
            if (currentByAchievementId.GetValueOrDefault(achievement.Id) < achievement.Threshold) continue;

            try
            {
                appDb.UserAchievements.Add(new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievement.Id,
                });

                var ownsSticker = await appDb.UserStickers
                    .AnyAsync(us => us.UserId == userId && us.StickerId == achievement.RewardStickerId);
                if (!ownsSticker)
                {
                    appDb.UserStickers.Add(new UserSticker
                    {
                        UserId = userId,
                        StickerId = achievement.RewardStickerId,
                    });
                }

                await appDb.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                appDb.ChangeTracker.Clear();
                continue;
            }

            logger.LogInformation(
                "Achievement {AchievementKey} unlocked by user {UserId} (reward sticker {StickerId})",
                achievement.Key, userId, achievement.RewardStickerId);

            try
            {
                await notificationService.SendNotificationAsync(new Notification
                {
                    UserId = userId,
                    Type = NotificationType.AchievementUnlocked,
                    Title = "Achievement unlocked!",
                    Message = $"{achievement.Name}: the {achievement.RewardSticker?.Name} sticker is yours.",
                    ReferenceId = achievement.Id,
                    ImageThumbnailUrl = achievement.RewardSticker?.ImageUrl,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send achievement-unlocked notification for {AchievementKey} to {UserId}",
                    achievement.Key, userId);
            }
        }
    }

    private async Task<int> CountMetricAsync(string userId, AchievementMetric metric)
    {
        return metric switch
        {
            AchievementMetric.ReviewsCreated => await appDb.Reviews
                .CountAsync(r => r.UserId == userId && !r.PingActivity!.Ping.IsDeleted),
            AchievementMetric.PingsCreated => await appDb.Pings
                .CountAsync(p => p.OwnerUserId == userId && !p.IsDeleted),
            AchievementMetric.Followers => await authDb.Follows
                .CountAsync(f => f.FolloweeId == userId),
            AchievementMetric.ReviewsWithTag => 0,
            _ => 0,
        };
    }

    /// <summary>Distinct reviews per tag name (case-insensitive; filter tags are lowercase).</summary>
    private async Task<Dictionary<string, int>> CountReviewsWithTagsAsync(string userId, IReadOnlyCollection<string> normalizedTags)
    {
        if (normalizedTags.Count == 0) return new Dictionary<string, int>();

        var grouped = await appDb.ReviewTags
            .AsNoTracking()
            .Where(rt =>
                normalizedTags.Contains(rt.Tag.Name.ToLower())
                && rt.Review.UserId == userId
                && !rt.Review.PingActivity!.Ping.IsDeleted)
            .GroupBy(rt => rt.Tag.Name.ToLower())
            .Select(g => new { Tag = g.Key, Count = g.Select(x => x.ReviewId).Distinct().Count() })
            .ToListAsync();

        return normalizedTags.ToDictionary(
            tag => tag,
            tag => grouped.FirstOrDefault(g => g.Tag == tag)?.Count ?? 0);
    }

    private static string? NormalizeFilterTag(AchievementMetric metric, string? filterTag)
    {
        if (metric != AchievementMetric.ReviewsWithTag)
        {
            if (!string.IsNullOrWhiteSpace(filterTag))
            {
                throw new ArgumentException("FilterTag is only used for ReviewsWithTag achievements.");
            }
            return null;
        }

        if (string.IsNullOrWhiteSpace(filterTag))
        {
            throw new ArgumentException("FilterTag is required for ReviewsWithTag achievements.");
        }

        return filterTag.Trim().ToLowerInvariant();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        return inner is not null
            && (inner.GetType().Name == "PostgresException" || inner.GetType().Name == "SqliteException")
            && (inner.Data["SqlState"] as string == "23505"
                || inner.Message.Contains("23505")
                || inner.Message.Contains("duplicate key")
                || inner.Message.Contains("UNIQUE constraint failed"));
    }

    // ==========================================
    // Admin CRUD
    // ==========================================

    public async Task<AdminAchievementDto> CreateAsync(UpsertAchievementDto dto)
    {
        var normalizedKey = dto.Key.Trim().ToLowerInvariant();
        if (await appDb.Achievements.AnyAsync(a => a.Key == normalizedKey))
        {
            throw new ArgumentException($"An achievement with key '{normalizedKey}' already exists.");
        }

        var sticker = await ResolveRewardStickerAsync(dto.RewardStickerId);
        var filterTag = NormalizeFilterTag(dto.Metric, dto.FilterTag);

        var achievement = new Achievement
        {
            Key = normalizedKey,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Metric = dto.Metric,
            FilterTag = filterTag,
            Threshold = dto.Threshold,
            RewardStickerId = sticker.Id,
            SortOrder = dto.SortOrder,
            IsActive = true,
        };

        appDb.Achievements.Add(achievement);
        await appDb.SaveChangesAsync();

        achievement.RewardSticker = sticker;
        return ToAdminDto(achievement, unlockCount: 0);
    }

    public async Task<AdminAchievementDto> UpdateAsync(string id, UpsertAchievementDto dto)
    {
        var achievement = await appDb.Achievements
            .Include(a => a.RewardSticker)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Achievement with ID {id} not found.");

        var sticker = await ResolveRewardStickerAsync(dto.RewardStickerId);
        var filterTag = NormalizeFilterTag(dto.Metric, dto.FilterTag);

        achievement.Name = dto.Name.Trim();
        achievement.Description = dto.Description?.Trim();
        achievement.Metric = dto.Metric;
        achievement.FilterTag = filterTag;
        achievement.Threshold = dto.Threshold;
        achievement.RewardStickerId = sticker.Id;
        achievement.RewardSticker = sticker;
        achievement.SortOrder = dto.SortOrder;

        await appDb.SaveChangesAsync();

        var unlockCount = await appDb.UserAchievements.CountAsync(ua => ua.AchievementId == id);
        return ToAdminDto(achievement, unlockCount);
    }

    public async Task<AdminAchievementDto> ToggleActiveAsync(string id)
    {
        var achievement = await appDb.Achievements
            .Include(a => a.RewardSticker)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Achievement with ID {id} not found.");

        achievement.IsActive = !achievement.IsActive;
        await appDb.SaveChangesAsync();

        var unlockCount = await appDb.UserAchievements.CountAsync(ua => ua.AchievementId == id);
        return ToAdminDto(achievement, unlockCount);
    }

    public async Task DeleteAsync(string id)
    {
        var achievement = await appDb.Achievements.FindAsync(id)
            ?? throw new KeyNotFoundException($"Achievement with ID {id} not found.");

        appDb.Achievements.Remove(achievement);
        await appDb.SaveChangesAsync();
    }

    public async Task<List<AdminAchievementDto>> GetAllForAdminAsync()
    {
        var unlockCounts = await appDb.UserAchievements
            .GroupBy(ua => ua.AchievementId)
            .Select(g => new { AchievementId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AchievementId, x => x.Count);

        var achievements = await appDb.Achievements
            .AsNoTracking()
            .Include(a => a.RewardSticker)
            .OrderBy(a => a.Metric).ThenBy(a => a.SortOrder).ThenBy(a => a.Threshold)
            .ToListAsync();

        return achievements
            .Select(a => ToAdminDto(a, unlockCounts.GetValueOrDefault(a.Id)))
            .ToList();
    }

    private async Task<Sticker> ResolveRewardStickerAsync(string stickerId)
    {
        var sticker = await appDb.Stickers.FindAsync(stickerId);
        if (sticker == null || !sticker.IsActive)
        {
            throw new ArgumentException($"Reward sticker '{stickerId}' not found or is inactive.");
        }
        return sticker;
    }

    private static AdminAchievementDto ToAdminDto(Achievement a, int unlockCount) => new(
        a.Id,
        a.Key,
        a.Name,
        a.Description,
        a.Metric,
        a.FilterTag,
        a.Threshold,
        a.RewardStickerId,
        a.RewardSticker?.Name ?? string.Empty,
        a.RewardSticker?.ImageUrl,
        a.IsActive,
        a.SortOrder,
        a.CreatedUtc,
        unlockCount);
}
