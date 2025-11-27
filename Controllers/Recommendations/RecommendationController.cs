using Conquest.Dtos.Recommendations;
using Conquest.Services.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Recommendations;

[ApiController]
[Route("api/[controller]")]
public class RecommendationController(RecommendationService recommendationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RecommendationDto>>> GetRecommendations(
        [FromQuery] string vibe, 
        [FromQuery] double latitude, 
        [FromQuery] double longitude,
        [FromQuery] double radius = 10.0) // Default 10km
    {
        if (string.IsNullOrWhiteSpace(vibe))
        {
            return BadRequest("Vibe is required.");
        }

        var results = await recommendationService.GetRecommendationsAsync(vibe, latitude, longitude, radius);
        return Ok(results);
    }
}
