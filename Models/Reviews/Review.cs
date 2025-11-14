using Conquest.Models.AppUsers;
using Conquest.Models.Places;

namespace Conquest.Models.Reviews;

public class Review
{
    public int Id { get; set; }

    public int PlaceId { get; set; }
    public string UserId { get; set; } = null!;

    public int Rating { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Place Place { get; set; } = null!;
    public string UserName { get; set; } = null!;
}
