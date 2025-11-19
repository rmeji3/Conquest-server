using Conquest.Dtos.Reviews;

namespace Conquest.Services.Reviews;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(int placeActivityId, CreateReviewDto dto, string userId, string userName);
    Task<IEnumerable<ReviewDto>> GetReviewsAsync(int placeActivityId, string scope, string userId);
}
