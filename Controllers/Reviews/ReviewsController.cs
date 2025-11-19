using System.Security.Claims;
using Conquest.Dtos.Reviews;
using Conquest.Services.Reviews;
using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Reviews;

[ApiController]
[Route("api/activities/{placeActivityId:int}/[controller]")]
public class ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview(int placeActivityId, [FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name;
        
        if (userId is null || userName is null)
        {
            logger.LogWarning("CreateReview: User is not authenticated or missing id/username.");
            return Unauthorized("User is not authenticated or missing id/username.");
        }

        try
        {
            var result = await reviewService.CreateReviewAsync(placeActivityId, dto, userId, userName);
            return CreatedAtAction(nameof(GetReviews), new { placeActivityId, scope = "mine" }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    

    // GET /api/activities/{placeActivityId}/reviews?scope=mine|global|friends
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int placeActivityId,
        [FromQuery] string scope = "global")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }
        
        try
        {
            var result = await reviewService.GetReviewsAsync(placeActivityId, scope, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
