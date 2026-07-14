using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ping.Data.App;
using Ping.Data.Auth;
using Ping.Dtos.Achievements;
using Ping.Models.Achievements;
using Ping.Models.Follows;
using Ping.Models.Notifications;
using Ping.Models.Pings;
using Ping.Models.Reviews;
using Ping.Models.Stickers;
using Ping.Services.Achievements;

namespace Ping.Tests.Controllers;

public class AchievementsTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    public AchievementsTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    /// <summary>Seeds a reward sticker and an achievement over it. Keys are made unique
    /// per call so tests sharing the factory database don't collide.</summary>
    private async Task<(string AchievementId, string StickerId)> SeedAchievementAsync(
        string keyPrefix, AchievementMetric metric, int threshold, string? filterTag = null)
    {
        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sticker = new Sticker { Key = $"{keyPrefix}_sticker", Name = $"{keyPrefix} prize" };
        appDb.Stickers.Add(sticker);

        var achievement = new Achievement
        {
            Key = $"{keyPrefix}_achievement",
            Name = $"{keyPrefix} milestone",
            Description = "Test achievement",
            Metric = metric,
            FilterTag = filterTag,
            Threshold = threshold,
            RewardStickerId = sticker.Id,
        };
        appDb.Achievements.Add(achievement);

        await appDb.SaveChangesAsync();
        return (achievement.Id, sticker.Id);
    }

    private async Task SeedReviewsWithTagsAsync(string userId, int count, string seedTag, params string[] tags)
    {
        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ping = new Models.Pings.Ping
        {
            Name = $"Achievement Test Place {seedTag}",
            Address = "1 Milestone Way",
            Location = new NetTopologySuite.Geometries.Point(-87.6298, 41.8781) { SRID = 4326 },
            OwnerUserId = "someone_else",
            Type = PingType.Custom,
        };
        var activity = new PingActivity { Ping = ping, Name = "General" };

        var tagEntities = new List<Tag>();
        foreach (var tagName in tags.Distinct())
        {
            var normalized = tagName.Trim().ToLowerInvariant();
            var tag = await appDb.Tags.FirstOrDefaultAsync(t => t.Name == normalized);
            if (tag == null)
            {
                tag = new Tag { Name = normalized };
                appDb.Tags.Add(tag);
            }
            tagEntities.Add(tag);
        }

        for (var i = 0; i < count; i++)
        {
            var review = new Review
            {
                UserId = userId,
                UserName = "achiever",
                PingActivity = activity,
                Rating = 5,
                Content = $"Visit {i}",
                ImageUrl = "https://example.com/image.jpg",
                ThumbnailUrl = "https://example.com/thumb.jpg",
            };
            foreach (var tag in tagEntities)
            {
                review.ReviewTags.Add(new ReviewTag { Tag = tag });
            }
            appDb.Reviews.Add(review);
        }

        await appDb.SaveChangesAsync();
    }

    private async Task<List<AchievementProgressDto>> GetAchievementsAsync()
    {
        var response = await Client.GetAsync("/api/achievements");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var achievements = JsonSerializer.Deserialize<List<AchievementProgressDto>>(body, JsonOptions);
        Assert.NotNull(achievements);
        return achievements!;
    }

    [Fact]
    public async Task GetAchievements_MeetsThreshold_ShouldUnlockGrantStickerAndNotify()
    {
        var userId = Authenticate("achiever_unlock");
        var (achievementId, stickerId) = await SeedAchievementAsync("ach_unlock", AchievementMetric.ReviewsCreated, 2);
        await SeedReviewsWithTagsAsync(userId, 2, "ach_unlock");

        var achievements = await GetAchievementsAsync();
        var unlocked = achievements.Single(a => a.Id == achievementId);

        Assert.NotNull(unlocked.UnlockedUtc);
        Assert.True(unlocked.Current >= unlocked.Threshold);
        Assert.Equal(stickerId, unlocked.RewardStickerId);

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await appDb.UserStickers.AnyAsync(us => us.UserId == userId && us.StickerId == stickerId));
        Assert.True(await appDb.UserAchievements.AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId));

        var notification = await appDb.Notifications.SingleAsync(n =>
            n.UserId == userId && n.Type == NotificationType.AchievementUnlocked);
        Assert.Equal(achievementId, notification.ReferenceId);
    }

    [Fact]
    public async Task GetAchievements_BelowThreshold_ShouldReportProgressWithoutUnlocking()
    {
        var userId = Authenticate("achiever_progress");
        var (achievementId, stickerId) = await SeedAchievementAsync("ach_progress", AchievementMetric.ReviewsCreated, 5);
        await SeedReviewsWithTagsAsync(userId, 2, "ach_progress");

        var achievements = await GetAchievementsAsync();
        var locked = achievements.Single(a => a.Id == achievementId);

        Assert.Null(locked.UnlockedUtc);
        Assert.Equal(2, locked.Current);
        Assert.Equal(5, locked.Threshold);

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await appDb.UserStickers.AnyAsync(us => us.UserId == userId && us.StickerId == stickerId));
    }

    [Fact]
    public async Task GetAchievements_CalledTwice_ShouldNotDuplicateUnlockOrNotification()
    {
        var userId = Authenticate("achiever_idempotent");
        var (achievementId, _) = await SeedAchievementAsync("ach_idem", AchievementMetric.ReviewsCreated, 1);
        await SeedReviewsWithTagsAsync(userId, 1, "ach_idem");

        await GetAchievementsAsync();
        await GetAchievementsAsync();

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await appDb.UserAchievements.CountAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId));
        Assert.Equal(1, await appDb.Notifications.CountAsync(n => n.UserId == userId && n.Type == NotificationType.AchievementUnlocked));
    }

    [Fact]
    public async Task CheckAndUnlock_PingsCreated_ShouldUnlock()
    {
        var userId = Authenticate("achiever_pings");
        var (achievementId, _) = await SeedAchievementAsync("ach_pings", AchievementMetric.PingsCreated, 1);

        using (var scope = Factory.Services.CreateScope())
        {
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDb.Pings.Add(new Models.Pings.Ping
            {
                Name = "My First Ping",
                Address = "2 Pioneer Rd",
                Location = new NetTopologySuite.Geometries.Point(-87.6298, 41.8781) { SRID = 4326 },
                OwnerUserId = userId,
                Type = PingType.Custom,
            });
            await appDb.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IAchievementService>();
            await service.CheckAndUnlockAsync(userId, AchievementMetric.PingsCreated);
        }

        using var assertScope = Factory.Services.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.UserAchievements.AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId));
    }

    [Fact]
    public async Task CheckAndUnlock_Followers_ShouldUnlockForFollowedUser()
    {
        var followedId = Guid.NewGuid().ToString();
        var (achievementId, _) = await SeedAchievementAsync("ach_followers", AchievementMetric.Followers, 2);

        using (var scope = Factory.Services.CreateScope())
        {
            var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            authDb.Follows.Add(new Follow { FollowerId = Guid.NewGuid().ToString(), FolloweeId = followedId });
            authDb.Follows.Add(new Follow { FollowerId = Guid.NewGuid().ToString(), FolloweeId = followedId });
            await authDb.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IAchievementService>();
            await service.CheckAndUnlockAsync(followedId, AchievementMetric.Followers);
        }

        using var assertScope = Factory.Services.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.UserAchievements.AnyAsync(ua => ua.UserId == followedId && ua.AchievementId == achievementId));
    }

    [Fact]
    public async Task GetAchievements_ReviewsWithTag_ShouldUnlockOnlyWhenTagged()
    {
        var userId = Authenticate("achiever_matcha");
        var (achievementId, stickerId) = await SeedAchievementAsync(
            "ach_matcha", AchievementMetric.ReviewsWithTag, 2, filterTag: "matcha");

        // Two reviews, but only one tagged matcha — should not unlock yet.
        await SeedReviewsWithTagsAsync(userId, 1, "ach_matcha_plain");
        await SeedReviewsWithTagsAsync(userId, 1, "ach_matcha_tagged", "matcha");

        var progress = await GetAchievementsAsync();
        var locked = progress.Single(a => a.Id == achievementId);
        Assert.Null(locked.UnlockedUtc);
        Assert.Equal(1, locked.Current);
        Assert.Equal("matcha", locked.FilterTag);

        await SeedReviewsWithTagsAsync(userId, 1, "ach_matcha_tagged2", "matcha");

        var unlockedList = await GetAchievementsAsync();
        var unlocked = unlockedList.Single(a => a.Id == achievementId);
        Assert.NotNull(unlocked.UnlockedUtc);
        Assert.True(unlocked.Current >= 2);

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await appDb.UserStickers.AnyAsync(us => us.UserId == userId && us.StickerId == stickerId));
    }

    [Fact]
    public async Task GetAchievements_ReviewsWithTag_IsCaseInsensitive()
    {
        var userId = Authenticate("achiever_matcha_caps");
        var (achievementId, stickerId) = await SeedAchievementAsync(
            "ach_matcha_caps", AchievementMetric.ReviewsWithTag, 1, filterTag: "matcha");

        using (var scope = Factory.Services.CreateScope())
        {
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ping = new Models.Pings.Ping
            {
                Name = "Achievement Caps Place",
                Address = "1 Case Lane",
                Location = new NetTopologySuite.Geometries.Point(-87.6298, 41.8781) { SRID = 4326 },
                OwnerUserId = "someone_else",
                Type = PingType.Custom,
            };
            var activity = new PingActivity { Ping = ping, Name = "General" };
            var tag = new Tag { Name = "Matcha" };
            appDb.Tags.Add(tag);
            var review = new Review
            {
                UserId = userId,
                UserName = "achiever",
                PingActivity = activity,
                Rating = 5,
                Content = "Great matcha",
                ImageUrl = "https://example.com/image.jpg",
                ThumbnailUrl = "https://example.com/thumb.jpg",
            };
            review.ReviewTags.Add(new ReviewTag { Tag = tag });
            appDb.Reviews.Add(review);
            await appDb.SaveChangesAsync();
        }

        var progress = await GetAchievementsAsync();
        var unlocked = progress.Single(a => a.Id == achievementId);
        Assert.NotNull(unlocked.UnlockedUtc);
        Assert.Equal(1, unlocked.Current);

        using var assertScope = Factory.Services.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.UserStickers.AnyAsync(us => us.UserId == userId && us.StickerId == stickerId));
    }

    [Fact]
    public async Task AdminCreate_ReviewsWithTagWithoutFilterTag_ShouldReturn400()
    {
        Authenticate("achievements_admin_tag", "Admin");

        string stickerId;
        using (var scope = Factory.Services.CreateScope())
        {
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sticker = new Sticker { Key = "ach_tag_sticker", Name = "Tag prize" };
            appDb.Stickers.Add(sticker);
            await appDb.SaveChangesAsync();
            stickerId = sticker.Id;
        }

        var response = await Client.PostAsJsonAsync("/api/admin/achievements", new
        {
            key = "ach_missing_tag",
            name = "Matcha",
            metric = "ReviewsWithTag",
            threshold = 1,
            rewardStickerId = stickerId,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminCrud_ShouldCreateUpdateToggleAndDelete()
    {
        Authenticate("achievements_admin", "Admin");

        // Reward sticker to point at.
        string stickerId;
        using (var scope = Factory.Services.CreateScope())
        {
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sticker = new Sticker { Key = "ach_admin_sticker", Name = "Admin prize" };
            appDb.Stickers.Add(sticker);
            await appDb.SaveChangesAsync();
            stickerId = sticker.Id;
        }

        // Create
        var createPayload = new
        {
            key = "ach_admin_crud",
            name = "Critic",
            description = "Write 25 reviews",
            metric = "ReviewsCreated",
            filterTag = (string?)null,
            threshold = 25,
            rewardStickerId = stickerId,
            sortOrder = 3,
        };
        var createResponse = await Client.PostAsJsonAsync("/api/admin/achievements", createPayload);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Create: {createBody}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<AdminAchievementDto>(createBody, JsonOptions)!;
        Assert.Equal("ach_admin_crud", created.Key);
        Assert.Equal(25, created.Threshold);
        Assert.True(created.IsActive);

        // Duplicate key rejected
        var duplicate = await Client.PostAsJsonAsync("/api/admin/achievements", createPayload);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        // List includes it
        var list = JsonSerializer.Deserialize<List<AdminAchievementDto>>(
            await Client.GetStringAsync("/api/admin/achievements"), JsonOptions)!;
        Assert.Contains(list, a => a.Id == created.Id);

        // Update threshold
        var updateResponse = await Client.PutAsJsonAsync($"/api/admin/achievements/{created.Id}", new
        {
            key = "ach_admin_crud",
            name = "Critic",
            description = "Write 30 reviews",
            metric = "ReviewsCreated",
            filterTag = (string?)null,
            threshold = 30,
            rewardStickerId = stickerId,
            sortOrder = 3,
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = JsonSerializer.Deserialize<AdminAchievementDto>(
            await updateResponse.Content.ReadAsStringAsync(), JsonOptions)!;
        Assert.Equal(30, updated.Threshold);

        // Toggle inactive
        var toggleResponse = await Client.PutAsync($"/api/admin/achievements/{created.Id}/toggle", null);
        Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);
        var toggled = JsonSerializer.Deserialize<AdminAchievementDto>(
            await toggleResponse.Content.ReadAsStringAsync(), JsonOptions)!;
        Assert.False(toggled.IsActive);

        // Delete
        var deleteResponse = await Client.DeleteAsync($"/api/admin/achievements/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = JsonSerializer.Deserialize<List<AdminAchievementDto>>(
            await Client.GetStringAsync("/api/admin/achievements"), JsonOptions)!;
        Assert.DoesNotContain(afterDelete, a => a.Id == created.Id);
    }

    [Fact]
    public async Task AdminCreate_WithUnknownSticker_ShouldReturn400()
    {
        Authenticate("achievements_admin_badsticker", "Admin");

        var response = await Client.PostAsJsonAsync("/api/admin/achievements", new
        {
            key = "ach_bad_sticker",
            name = "Broken",
            metric = "ReviewsCreated",
            threshold = 1,
            rewardStickerId = "does-not-exist",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_AsRegularUser_ShouldReturn403()
    {
        Authenticate("achievements_regular_user");

        var response = await Client.GetAsync("/api/admin/achievements");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
