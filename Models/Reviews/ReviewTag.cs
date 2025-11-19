namespace Conquest.Models.Reviews;

public class ReviewTag
{
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}