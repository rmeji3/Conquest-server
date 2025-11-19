using Conquest.Dtos.Friends;
using Conquest.Services.Friends;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Conquest.Controllers
{
    [Route("api/[controller]")]
    public class FriendsController(IFriendService friendService) : ControllerBase
    {
        [Authorize]
        [HttpGet("friends")]
        public async Task<IActionResult> GetMyFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var friends = await friendService.GetMyFriendsAsync(userId);
            return Ok(friends);
        }

        [Authorize]
        [HttpPost("add/{username}")]
        public async Task<IActionResult> AddFriend(string username)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            try
            {
                var result = await friendService.AddFriendAsync(userId, username);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("accept/{username}")]
        public async Task<IActionResult> AcceptFriend(string username)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            try
            {
                var result = await friendService.AcceptFriendAsync(userId, username);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("requests/incoming")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var requests = await friendService.GetIncomingRequestsAsync(userId);
            return Ok(requests);
        }

        [Authorize]
        [HttpPost("remove/{username}")]
        public async Task<IActionResult> RemoveFriend(string username)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            try
            {
                var result = await friendService.RemoveFriendAsync(userId, username);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
