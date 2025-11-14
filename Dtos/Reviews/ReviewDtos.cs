namespace Conquest.Dtos.Reviews;

public record ReviewDto(int Id, int Rating, string Content, string UserName, DateTime CreatedAt);
public record CreateReviewDto(int Rating, string Content);