using Ping.Models.AppUsers;

namespace Ping.Models.Users
{
    public class UserBlock
    {
        public string BlockerId { get; set; } = null!;
        public AppUser Blocker { get; set; } = null!;

        public string BlockedId { get; set; } = null!;
        public AppUser Blocked { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

