using Conquest.Data.App;
using Conquest.Dtos.Reviews;
using Conquest.Models.Reviews;
using Conquest.Services.Friends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Reviews;

public class ReviewService(
    AppDbContext appDb,
    IFriendService friendService,
    ILogger<ReviewService> logger) : IReviewService
{
    public async Task<ReviewDto> CreateReviewAsync(int placeActivityId, CreateReviewDto dto, string userId, string userName)
    {
        // Ensure activity exists
        var activityExists = await appDb.PlaceActivities
            .AnyAsync(pa => pa.Id == placeActivityId);

        if (!activityExists)
        {
            logger.LogWarning("CreateReview: Activity {PlaceActivityId} not found.", placeActivityId);
            throw new KeyNotFoundException("Activity not found.");
        }

        var review = new Review
        {
            PlaceActivityId = placeActivityId,
            UserId = userId,
            UserName = userName,
            Rating = dto.Rating,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow,
        };

        appDb.Reviews.Add(review);
        await appDb.SaveChangesAsync();

        logger.LogInformation("Review created for Activity {PlaceActivityId} by {UserName}. Rating: {Rating}", placeActivityId, userName, dto.Rating);

        return new ReviewDto(
            review.Id,
            review.Rating,
            review.Content,
            review.UserName,
            review.CreatedAt
        );
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsAsync(int placeActivityId, string scope, string userId)
    {
        var activityExists = await appDb.PlaceActivities
            .AnyAsync(pa => pa.Id == placeActivityId);
        if (!activityExists)
            throw new KeyNotFoundException("Activity not found.");

        var query = appDb.Reviews
            .AsNoTracking()
            .Where(r => r.PlaceActivityId == placeActivityId)
            .OrderByDescending(r => r.CreatedAt)
            .AsQueryable();

        switch (scope.ToLowerInvariant())
        {
            case "mine":
                {
                    query = query.Where(r => r.UserId == userId);
                    break;
                }
            case "friends":
                {
                    var friendIds = await friendService.GetFriendIdsAsync(userId);
                    if (friendIds.Count == 0)
                    {
                        // no friends → no reviews in this scope
                        return Array.Empty<ReviewDto>();
                    }
                    query = query.Where(r => friendIds.Contains(r.UserId));
                    break;
                }
            case "global":
            default:
                // no extra filter
                break;
        }

        return await query
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Content,
                r.UserName,
                r.CreatedAt
            ))
            .ToListAsync();
    }
}
