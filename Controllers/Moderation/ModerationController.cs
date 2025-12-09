using System.Security.Claims;
using Conquest.Services.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Moderation
{
    [ApiController]
    [Route("api/moderation")]
    [Authorize(Roles = "Admin")]
    public class ModerationController(IBanningService banningService) : ControllerBase
    {
        [HttpPost("ban/user/{id}")]
        public async Task<IActionResult> BanUser(string id, [FromQuery] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest("Reason is required.");

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await banningService.BanUserAsync(id, reason, adminId);
            return Ok(new { message = $"User {id} banned." });
        }

        [HttpPost("unban/user/{id}")]
        public async Task<IActionResult> UnbanUser(string id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await banningService.UnbanUserAsync(id, adminId);
            return Ok(new { message = $"User {id} unbanned." });
        }

        [HttpPost("ban/ip")]
        public async Task<IActionResult> BanIp([FromBody] IpBanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Ip) || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("IP and Reason are required.");

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await banningService.BanIpAsync(request.Ip, request.Reason, request.ExpiresAt, adminId);
            return Ok(new { message = $"IP {request.Ip} banned." });
        }

        [HttpPost("unban/ip")]
        public async Task<IActionResult> UnbanIp([FromBody] IpUnbanRequest request)
        {
             if (string.IsNullOrWhiteSpace(request.Ip))
                return BadRequest("IP is required.");

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await banningService.UnbanIpAsync(request.Ip, adminId);
            return Ok(new { message = $"IP {request.Ip} unbanned." });
        }
    }

    public class IpBanRequest
    {
        public required string Ip { get; set; }
        public required string Reason { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class IpUnbanRequest
    {
         public required string Ip { get; set; }
    }
}
