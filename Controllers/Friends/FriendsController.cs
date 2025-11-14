using Conquest.Data.App;
using Conquest.Models.AppUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Conquest.Models.Friends;
using Conquest.Dtos.Friends;
using Conquest.Data.Auth;

namespace Conquest.Controllers
{
    [Route("api/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AuthDbContext _context;

        public FriendsController(AuthDbContext appContext, UserManager<AppUser> userManager)
        {
            _context = appContext;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet("friends")]
        public async Task<IActionResult> GetMyFriends()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null) return Unauthorized();

            var friends = await _context.Friendships
                .Where(f => f.UserId == me.Id && f.Status == Friendship.FriendshipStatus.Accepted)
                .Select(f => new FriendSummaryDto
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName!,     // choose only what you want exposed
                    FirstName = f.Friend.FirstName!,
                    LastName = f.Friend.LastName!
                })
                .ToListAsync();

            return Ok(friends);
        }

        [Authorize]
        [HttpPost("add/{username}")]
        public async Task<IActionResult> AddFriend(string username)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null) return Unauthorized();

            var friend = await _userManager.FindByNameAsync(username);

            if (friend is null) return NotFound("User not found.");

            // can't add yourself
            if (friend.Id == me.Id) return BadRequest("You cannot add yourself as a friend.");

            var userId = me.Id;
            var friendId = friend.Id;

            // if we already have any Accepted link either direction, they're friends
            var alreadyFriends = await _context.Friendships.AnyAsync(f =>
                ((f.UserId == userId && f.FriendId == friendId) ||
                 (f.UserId == friendId && f.FriendId == userId)) &&
                f.Status == Friendship.FriendshipStatus.Accepted);

            if (alreadyFriends) return Conflict("Already friends.");

            // did I already send a pending request to them?
            var existingOutgoing = await _context.Friendships.FirstOrDefaultAsync(f =>
                f.UserId == userId &&
                f.FriendId == friendId &&
                f.Status == Friendship.FriendshipStatus.Pending);

            if (existingOutgoing is not null)
                return BadRequest("You already sent a friend request to this user.");

            // did THEY already send a pending request to ME?
            var existingIncoming = await _context.Friendships.FirstOrDefaultAsync(f =>
                f.UserId == friendId &&
                f.FriendId == userId &&
                f.Status == Friendship.FriendshipStatus.Pending);

            if (existingIncoming is not null)
                return BadRequest("This user already sent you a request. Accept it instead.");

            // have they blocked me?
            var blocked = await _context.Friendships.FirstOrDefaultAsync(f =>
                f.UserId == friendId &&
                f.FriendId == userId &&
                f.Status == Friendship.FriendshipStatus.Blocked);

            if (blocked is not null)
                return BadRequest("This user has blocked you. You cannot send a request.");

            // create a new pending request (one row: me -> friend)
            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = Friendship.FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok("Friend request sent!");
        }

        [Authorize]
        [HttpPost("accept/{username}")]
        public async Task<IActionResult> AcceptFriend(string username)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null) { return Unauthorized(); }

            var friend = await _userManager.FindByNameAsync(username);
            if (friend is null) { return NotFound("User not found."); }

            var userId = me.Id;
            var friendId = friend.Id;

            // find pending request sent by THEM to ME
            var pendingRequest = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.UserId == friendId &&
                    f.FriendId == userId &&
                    f.Status == Friendship.FriendshipStatus.Pending);

            if (pendingRequest is null)
                return BadRequest("No pending request from this user.");

            // update the request to Accepted
            pendingRequest.Status = Friendship.FriendshipStatus.Accepted;

            // also add reverse link if it doesn’t exist yet
            var reverseExists = await _context.Friendships.AnyAsync(f =>
                f.UserId == userId &&
                f.FriendId == friendId &&
                f.Status == Friendship.FriendshipStatus.Accepted);

            if (!reverseExists)
            {
                _context.Friendships.Add(new Friendship
                {
                    UserId = userId,
                    FriendId = friendId,
                    Status = Friendship.FriendshipStatus.Accepted,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok("Friend request accepted.");
        }

        [Authorize]
        [HttpPost("requests/incoming")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null) return Unauthorized();
            var requests = await _context.Friendships
                .Where(f => f.FriendId == me.Id && f.Status == Friendship.FriendshipStatus.Pending)
                .Select(f => new FriendSummaryDto
                {
                    Id = f.User.Id,
                    UserName = f.User.UserName!,
                    FirstName = f.User.FirstName!,
                    LastName = f.User.LastName!
                })
                .ToListAsync();
            return Ok(requests);
        }

        [Authorize]
        [HttpPost("remove/{username}")]
        public async Task<IActionResult> RemoveFriend(string username)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null) return Unauthorized();
            var friend = await _userManager.FindByNameAsync(username);
            if (friend is null) return NotFound("User not found.");
            var userId = me.Id;
            var friendId = friend.Id;
            var friendship1 = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId && f.Status == Friendship.FriendshipStatus.Accepted);
            var friendship2 = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == userId && f.Status == Friendship.FriendshipStatus.Accepted);
            if (friendship1 is null && friendship2 is null)
                return BadRequest("You are not friends with this user.");
            if (friendship1 is not null)
                _context.Friendships.Remove(friendship1);
            if (friendship2 is not null)
                _context.Friendships.Remove(friendship2);
            await _context.SaveChangesAsync();
            return Ok("Friend removed.");
        }
    }
}
