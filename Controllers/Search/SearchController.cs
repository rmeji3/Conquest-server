using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Ping.Dtos.Search;
using Ping.Services.Search;

namespace Ping.Controllers.Search;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SearchController(ISearchService searchService) : ControllerBase
{
    /// <summary>
    /// Unified search across Profiles, Pings, and Events.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<UnifiedSearchResultDto>> UnifiedSearch([FromQuery] UnifiedSearchFilterDto filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        try
        {
            var results = await searchService.UnifiedSearchAsync(filter, userId);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
