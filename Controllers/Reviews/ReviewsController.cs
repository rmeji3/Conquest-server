using System.Security.Claims;
using Conquest.Data.App;
using Conquest.Dtos.Reviews;
using Conquest.Models.Reviews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conquest.Controllers.Reviews;

[ApiController]
[Route("api/activities/{placeActivityId:int}/[controller]")]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview(int placeActivityId, [FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name;

        if (userId is null || string.IsNullOrWhiteSpace(userName))
        {
            return Unauthorized("User is not authenticated or missing id/username.");
        }

        // Ensure activity exists
        var activityExists = await db.PlaceActivities
            .AnyAsync(pa => pa.Id == placeActivityId);

        if (!activityExists)
            return NotFound("Activity not found.");

        // One review per user per activity
        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.PlaceActivityId == placeActivityId && r.UserId == userId);

        if (alreadyReviewed)
        {
            return Conflict("You’ve already left a review for this activity. You can edit it instead.");
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

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var result = new ReviewDto(
            review.Id,
            review.Rating,
            review.Content,
            review.UserName,
            review.CreatedAt
        );

        // 201 Created is nicer than 200 for POST
        return CreatedAtAction(nameof(GetReviews), new { placeActivityId }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int placeActivityId)
    {
        var activityExists = await db.PlaceActivities
            .AnyAsync(pa => pa.Id == placeActivityId);

        if (!activityExists)
            return NotFound("Activity not found.");

        var reviews = await db.Reviews
            .Where(r => r.PlaceActivityId == placeActivityId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Content!,
                r.UserName,
                r.CreatedAt
            ))
            .ToListAsync();

        return Ok(reviews);
    }
}
