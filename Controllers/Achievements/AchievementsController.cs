using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ping.Dtos.Achievements;
using Ping.Services.Achievements;

namespace Ping.Controllers.Achievements;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AchievementsController(IAchievementService achievementService) : ControllerBase
{
    // GET /api/achievements — active achievements with the caller's progress.
    // Also lazily unlocks anything the user already qualifies for.
    [HttpGet]
    public async Task<ActionResult<List<AchievementProgressDto>>> GetMyAchievements()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        return Ok(await achievementService.GetMyAchievementsAsync(userId));
    }
}
