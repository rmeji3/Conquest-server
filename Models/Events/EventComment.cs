using Ping.Models.AppUsers;
using System.ComponentModel.DataAnnotations;

namespace Ping.Models.Events;

public class EventComment
{
    public int Id { get; set; }

    [MaxLength(500)]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK to Event
    public int EventId { get; set; }
    public Event? Event { get; set; }

    // FK to User
    public required string UserId { get; set; }
    public AppUser? User { get; set; }
}
