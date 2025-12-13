using Ping.Models.AppUsers;

namespace Ping.Models.Friends
{
    public class Friendship
    {
        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public string FriendId { get; set; } = null!;
        public AppUser Friend { get; set; } = null!;

        public enum FriendshipStatus { Pending, Accepted, Blocked }
        public FriendshipStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

