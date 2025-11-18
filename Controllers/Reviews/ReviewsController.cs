using System.Security.Claims;
using Conquest.Data.App;
using Conquest.Dtos.Reviews;
using Conquest.Models.Reviews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conquest.Controllers.ReviewsController;
[ApiController]
[Route("api/Places/{placeId:int}/[controller]")]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview( int placeId, CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name;

        if (userId is null || string.IsNullOrWhiteSpace(userName))
        {
            return Unauthorized("User is not authenticated or missing id/username.");
        }
        
        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.PlaceId == placeId && r.UserId == userId);

        if (alreadyReviewed)
        {
            // 409 = Conflict (good for "you already did this" situations)
            return Conflict("You’ve already left a review for this place. You can edit it instead.");
        }
        var review = new Review
        {
            PlaceId = placeId,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            UserName = User.Identity!.Name!,  
            Rating = dto.Rating,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow,
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        return Ok(new ReviewDto(
            review.Id, review.Rating, review.Content, User.Identity!.Name!, review.CreatedAt
        ));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int placeId)
    {
        return Ok(await db.Reviews
            .Where(r => r.PlaceId == placeId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Content,
                r.UserName,
                r.CreatedAt
            ))
            .ToListAsync());
    }

}