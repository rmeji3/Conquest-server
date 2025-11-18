using System.ComponentModel.DataAnnotations;

namespace Conquest.Models.Places;

public class CheckIn
{
    public int Id { get; set; }
    [MaxLength(200)]
    public string UserId { get; set; } = null!;
    public int PlaceActivityId { get; set; }
    public PlaceActivity PlaceActivity { get; set; } = null!;

    [MaxLength(500)]
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
