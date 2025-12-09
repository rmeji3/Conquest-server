using Conquest.Dtos.Tags;
using Conquest.Services.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Tags;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require auth for everything, maybe allow anonymous for search/popular later?
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet("popular")]
    [AllowAnonymous] // Public can see popular tags
    public async Task<ActionResult<IEnumerable<TagDto>>> GetPopularTags([FromQuery] int count = 20)
    {
        var tags = await tagService.GetPopularTagsAsync(count);
        return Ok(tags);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TagDto>>> SearchTags([FromQuery] string q, [FromQuery] int count = 20)
    {
        var tags = await tagService.SearchTagsAsync(q, count);
        return Ok(tags);
    }


}
