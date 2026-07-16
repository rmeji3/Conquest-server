using Xunit;
using Xunit.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ping.Data.App;
using Ping.Dtos.Reviews;
using Ping.Models.Pings;

namespace Ping.Tests.Controllers;

public class ReviewsControllerTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public ReviewsControllerTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    /// <summary>Seeds a ping with one activity and returns the activity id.</summary>
    private async Task<int> SeedActivityAsync(string ownerUserId, string name)
    {
        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activity = new PingActivity
        {
            Name = "General",
            Ping = new Models.Pings.Ping
            {
                Name = name,
                Address = "123 Idempotency St",
                Location = new NetTopologySuite.Geometries.Point(-87.6298, 41.8781) { SRID = 4326 },
                OwnerUserId = ownerUserId,
                Type = PingType.Custom,
            },
        };
        appDb.PingActivities.Add(activity);
        await appDb.SaveChangesAsync();
        return activity.Id;
    }

    /// <summary>Posts a minimal review (no content/images, so no moderation or S3 involved).</summary>
    private Task<HttpResponseMessage> PostReviewAsync(int activityId, string? clientRequestId)
    {
        var form = new MultipartFormDataContent { { new StringContent("5"), "Rating" } };
        if (clientRequestId != null)
        {
            form.Add(new StringContent(clientRequestId), "ClientRequestId");
        }
        return Client.PostAsync($"/api/ping-activities/{activityId}/reviews", form);
    }

    [Fact]
    public async Task CreateReview_RetriedWithSameClientRequestId_ReturnsExistingReview()
    {
        // Arrange
        var userId = Authenticate("user_idempotent");
        var activityId = await SeedActivityAsync(userId, "Idempotent Place");
        var clientRequestId = Guid.NewGuid().ToString();

        // Act — the retry a client fires when it never saw the first response
        var first = await PostReviewAsync(activityId, clientRequestId);
        var second = await PostReviewAsync(activityId, clientRequestId);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstDto = await first.Content.ReadFromJsonAsync<ReviewDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(firstDto);
        Assert.NotNull(secondDto);
        Assert.Equal(firstDto!.Id, secondDto!.Id);

        using var scope = Factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await appDb.Reviews.CountAsync(r => r.PingActivityId == activityId && r.UserId == userId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateReview_WithDifferentClientRequestIds_CreatesSeparateReviews()
    {
        // Arrange
        var userId = Authenticate("user_two_reviews");
        var activityId = await SeedActivityAsync(userId, "Two Reviews Place");

        // Act
        var first = await PostReviewAsync(activityId, Guid.NewGuid().ToString());
        var second = await PostReviewAsync(activityId, Guid.NewGuid().ToString());

        // Assert — distinct submissions still create distinct reviews (check-ins)
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var firstDto = await first.Content.ReadFromJsonAsync<ReviewDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotEqual(firstDto!.Id, secondDto!.Id);
    }

    [Fact]
    public async Task CreateReview_WithoutClientRequestId_KeepsLegacyBehavior()
    {
        // Arrange — old app versions don't send the key at all
        var userId = Authenticate("user_legacy");
        var activityId = await SeedActivityAsync(userId, "Legacy Place");

        // Act
        var first = await PostReviewAsync(activityId, null);
        var second = await PostReviewAsync(activityId, null);

        // Assert — both create (null keys must never collide on the unique index)
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var firstDto = await first.Content.ReadFromJsonAsync<ReviewDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotEqual(firstDto!.Id, secondDto!.Id);
    }

    [Fact]
    public async Task GetExploreReviews_WithRadiusKmButNoCoordinates_ShouldNotFail()
    {
        // Arrange
        var userId = Authenticate("user_explore");

        // Act
        var response = await Client.GetAsync("/api/reviews/explore?radiusKm=160.93&scope=global");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetExploreReviews_WithCoordinates_ShouldNotThrow500()
    {
        // Arrange
        var userId = Authenticate("user_explore_2");

        // Act
        var response = await Client.GetAsync("/api/reviews/explore?latitude=41.8781&longitude=-87.6298&radiusKm=160.93&scope=global");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
