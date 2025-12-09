using System.ComponentModel.DataAnnotations;
using Conquest.Models.AppUsers;

namespace Conquest.Models.Analytics
{
    public class UserActivityLog
    {
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }
        public AppUser? User { get; set; }

        public DateOnly Date { get; set; }

        public int LoginCount { get; set; } = 1;

        public DateTimeOffset LastActivityUtc { get; set; }
    }
}
