using System.Security.Claims;
using Conquest.Dtos.Profiles;
using Conquest.Models.AppUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conquest.Controllers.Profiles;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController(UserManager<AppUser> userManager) : ControllerBase
{
    // GET /api/profiles/me
    [HttpGet("me")]
    public async Task<ActionResult<PersonalProfileDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var profile = new PersonalProfileDto(
            user.Id,
            user.UserName!,           // ensure not null
            user.FirstName,
            user.LastName,
            user.ProfileImageUrl,
            user.Email!
        );
        
        return Ok(profile);
    }
    
    // GET /api/profiles/search?username=someUsername
    [HttpGet("search")]
    public async Task<ActionResult<List<ProfileDto>>> Search([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Username query parameter is required.");

        var normalized = username.ToUpper(); // match Identity normalization
        
        var yourUsername = User.FindFirstValue(ClaimTypes.Name);

        var users = await userManager.Users
            .Where(u => u.NormalizedUserName!.StartsWith(normalized)
            && !u.NormalizedUserName.Equals(yourUsername, StringComparison.CurrentCultureIgnoreCase)) // exclude yourself
            .OrderBy(u => u.UserName)
            .Take(15)
            .Select(u => new ProfileDto(
                u.Id,
                u.UserName!,
                u.FirstName,
                u.LastName,
                u.ProfileImageUrl
            ))
            .ToListAsync();

        return Ok(users);
    }
}