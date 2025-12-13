namespace Ping.Dtos.Reviews;

public record UserReviewsDto(
    ReviewDto Review,
    List<ReviewDto> History
);

