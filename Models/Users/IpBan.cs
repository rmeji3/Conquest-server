using System.ComponentModel.DataAnnotations;

namespace Conquest.Models.Users
{
    public class IpBan
    {
        [Key]
        [MaxLength(45)] // IPv6 support
        public string IpAddress { get; set; } = string.Empty;
        
        public string Reason { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
    }
}
