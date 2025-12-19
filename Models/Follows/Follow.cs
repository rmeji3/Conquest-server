using Ping.Models.AppUsers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ping.Models.Follows
{
    public class Follow
    {
        public string FollowerId { get; set; } = null!;
        [ForeignKey("FollowerId")]
        public AppUser Follower { get; set; } = null!;

        public string FolloweeId { get; set; } = null!;
        [ForeignKey("FolloweeId")]
        public AppUser Followee { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
