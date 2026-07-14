using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ping.Data.App;
using Ping.Dtos.Reviews;
using Ping.Models.Notifications;
using Ping.Models.Pings;
using Ping.Models.Reviews;
using Ping.Models.Stickers;

namespace Ping.Tests.Controllers;

public class ReviewReactionsTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public ReviewReactionsTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    /// <summary>
    /// Seeds a review (with its ping + activity), a catalog sticker, and — when
    /// <paramref name="ownerUserId"/> is provided — ownership of that sticker.
    /// Returns the review id and sticker id.
    /// </summary>
    private async Task<(int ReviewId, string StickerId)> SeedReviewAndStickerAsync(
        string authorUserId, string stickerKey, string? ownerUserId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ping = new Models.Pings.Ping
        {
            Name = $"Reaction Test Place {stickerKey}",
            Address = "123 Reaction St",
            Location = new NetTopologySuite.Geometries.Point(-87.6298, 41.8781) { SRID = 4326 },
            OwnerUserId = authorUserId,
            Type = PingType.Custom,
        };
        var activity = new PingActivity { Ping = ping, Name = "General" };
        var review = new Review
        {
            UserId = authorUserId,
            UserName = "review_author",
            PingActivity = activity,
            Rating = 5,
            Content = "Great spot!",
            ImageUrl = "https://example.com/image.jpg",
            ThumbnailUrl = "https://example.com/thumb.jpg",
        };
        appDb.Reviews.Add(review);

        var sticker = await appDb.Stickers.SingleOrDefaultAsync(s => s.Key == stickerKey);
        if (sticker == null)
        {
            sticker = new Sticker { Key = stickerKey, Name = $"Sticker {stickerKey}" };
            appDb.Stickers.Add(sticker);
        }

        if (ownerUserId != null &&
            !await appDb.UserStickers.AnyAsync(us => us.UserId == ownerUserId && us.StickerId == sticker.Id))
        {
            appDb.UserStickers.Add(new UserSticker { UserId = ownerUserId, StickerId = sticker.Id });
        }

        await appDb.SaveChangesAsync();
        return (review.Id, sticker.Id);
    }

    private async Task<string> SeedOwnedStickerAsync(string ownerUserId, string stickerKey)
    {
        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sticker = new Sticker { Key = stickerKey, Name = $"Sticker {stickerKey}" };
        appDb.Stickers.Add(sticker);
        appDb.UserStickers.Add(new UserSticker { UserId = ownerUserId, StickerId = sticker.Id });
        await appDb.SaveChangesAsync();
        return sticker.Id;
    }

    private Task<HttpResponseMessage> AddReactionAsync(int reviewId, string stickerId) =>
        Client.PostAsJsonAsync($"/api/reviews/{reviewId}/reactions", new AddReviewReactionDto(stickerId));

    private async Task<List<ReviewReactionDto>> GetReactionsAsync(int reviewId)
    {
        var response = await Client.GetAsync($"/api/reviews/{reviewId}/reactions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reactions = await response.Content.ReadFromJsonAsync<List<ReviewReactionDto>>();
        Assert.NotNull(reactions);
        return reactions!;
    }

    [Fact]
    public async Task AddReaction_DuplicateSticker_ShouldReturn400()
    {
        var userId = Authenticate("reactor_duplicate");
        var (reviewId, stickerId) = await SeedReviewAndStickerAsync(
            "author_1", "react_duplicate", ownerUserId: userId);

        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, stickerId)).StatusCode);

        var duplicate = await AddReactionAsync(reviewId, stickerId);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var body = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("already reacted", body);

        var reactions = await GetReactionsAsync(reviewId);
        var reaction = Assert.Single(reactions);
        Assert.Equal(stickerId, reaction.StickerId);
        Assert.Equal(1, reaction.Count);
        Assert.Equal(1, reaction.MyCount);
    }

    [Fact]
    public async Task AddReaction_WithUnownedSticker_ShouldReturn400()
    {
        var userId = Authenticate("reactor_unowned");
        var (reviewId, stickerId) = await SeedReviewAndStickerAsync("author_2", "react_unowned");

        var response = await AddReactionAsync(reviewId, stickerId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {body}");
        Assert.Contains("stickers you own", body);
    }

    [Theory]
    [InlineData("verified_badge")]
    [InlineData("founder_badge")]
    public async Task AddReaction_WithProfileBadge_ShouldReturn400(string badgeKey)
    {
        var userId = Authenticate($"reactor_{badgeKey}");
        var (reviewId, stickerId) = await SeedReviewAndStickerAsync(
            $"author_{badgeKey}", badgeKey, ownerUserId: userId);

        var response = await AddReactionAsync(reviewId, stickerId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("badges cannot be used", body);
        Assert.Empty(await GetReactionsAsync(reviewId));
    }

    [Fact]
    public async Task AddReaction_OnMissingReview_ShouldReturn404()
    {
        var userId = Authenticate("reactor_missing_review");
        var (_, stickerId) = await SeedReviewAndStickerAsync("author_3", "react_missing", ownerUserId: userId);

        var response = await AddReactionAsync(999999, stickerId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddReaction_BeyondCap_ShouldReturn400()
    {
        var userId = Authenticate("reactor_cap");
        var (reviewId, firstStickerId) = await SeedReviewAndStickerAsync("author_4", "react_cap_a", ownerUserId: userId);
        var stickerIds = new List<string> { firstStickerId };
        for (var i = 1; i <= 10; i++)
        {
            stickerIds.Add(await SeedOwnedStickerAsync(userId, $"react_cap_{i}"));
        }

        // Ten different stickers fill the budget.
        foreach (var stickerId in stickerIds.Take(10))
        {
            Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, stickerId)).StatusCode);
        }

        // An eleventh different sticker is rejected.
        var overCap = await AddReactionAsync(reviewId, stickerIds[10]);
        Assert.Equal(HttpStatusCode.BadRequest, overCap.StatusCode);
        var body = await overCap.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {body}");
        Assert.Contains("10", body);

        var reactions = await GetReactionsAsync(reviewId);
        Assert.Equal(10, reactions.Count);
        Assert.All(reactions, reaction =>
        {
            Assert.Equal(1, reaction.Count);
            Assert.Equal(1, reaction.MyCount);
        });
    }

    [Fact]
    public async Task RemoveReactions_ByStickerAndAll_ShouldClear()
    {
        var userId = Authenticate("reactor_remove");
        var (reviewId, firstStickerId) = await SeedReviewAndStickerAsync("author_5", "react_rm_a", ownerUserId: userId);
        var secondStickerId = await SeedOwnedStickerAsync(userId, "react_rm_b");

        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, firstStickerId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, secondStickerId)).StatusCode);

        // Remove only the first sticker.
        var removeOne = await Client.DeleteAsync($"/api/reviews/{reviewId}/reactions?stickerId={firstStickerId}");
        Assert.Equal(HttpStatusCode.OK, removeOne.StatusCode);

        var afterOne = await GetReactionsAsync(reviewId);
        var remaining = Assert.Single(afterOne);
        Assert.Equal(secondStickerId, remaining.StickerId);

        // Remove everything; removing again stays idempotent.
        var removeAll = await Client.DeleteAsync($"/api/reviews/{reviewId}/reactions");
        Assert.Equal(HttpStatusCode.OK, removeAll.StatusCode);
        Assert.Empty(await GetReactionsAsync(reviewId));

        var removeAgain = await Client.DeleteAsync($"/api/reviews/{reviewId}/reactions");
        Assert.Equal(HttpStatusCode.OK, removeAgain.StatusCode);
    }

    [Fact]
    public async Task AddReaction_ShouldNotifyAuthorOnlyOnce()
    {
        var userId = Authenticate("reactor_notify");
        var (reviewId, stickerId) = await SeedReviewAndStickerAsync("author_notify", "react_notify", ownerUserId: userId);
        var secondStickerId = await SeedOwnedStickerAsync(userId, "react_notify_second");

        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, stickerId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AddReactionAsync(reviewId, stickerId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, secondStickerId)).StatusCode);

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationCount = await appDb.Notifications.CountAsync(n =>
            n.UserId == "author_notify" && n.Type == NotificationType.ReviewStickerReaction);

        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public async Task ExploreFeed_ShouldIncludeReactions()
    {
        var userId = Authenticate("reactor_feed");
        var (reviewId, stickerId) = await SeedReviewAndStickerAsync("author_6", "react_feed", ownerUserId: userId);

        Assert.Equal(HttpStatusCode.OK, (await AddReactionAsync(reviewId, stickerId)).StatusCode);

        var response = await Client.GetAsync("/api/reviews/explore?scope=global&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {body}");

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();
        var reviewJson = items.FirstOrDefault(i => i.GetProperty("reviewId").GetInt32() == reviewId);
        Assert.NotEqual(default, reviewJson.ValueKind);

        var reactions = reviewJson.GetProperty("reactions").EnumerateArray().ToList();
        var reaction = Assert.Single(reactions);
        Assert.Equal(stickerId, reaction.GetProperty("stickerId").GetString());
        Assert.Equal(1, reaction.GetProperty("count").GetInt32());
        Assert.Equal(1, reaction.GetProperty("myCount").GetInt32());
    }
}
