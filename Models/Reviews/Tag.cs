namespace Ping.Models.Reviews;

public class Tag
{
    public int Id { get; set; }

    public required string Name { get; set; }   // normalized (lowercase, trimmed)
    public int? CanonicalTagId { get; set; }    // for merging synonyms
    public Tag? CanonicalTag { get; set; }

    public bool IsBanned { get; set; }
    public bool IsApproved { get; set; } = true;

    // nav
    public List<ReviewTag> ReviewTags { get; set; } = [];
}

