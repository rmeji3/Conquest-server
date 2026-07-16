using Ping.Dtos.Common;
using Ping.Dtos.Reviews;

namespace Ping.Services.Reviews;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(int pingActivityId, CreateReviewDto dto, string userId, string userName);

    /// <summary>
    /// The user's review created with this idempotency key, or null if none.
    /// Lets a retried create (same <c>ClientRequestId</c>) return the original
    /// review — callers check this before doing any per-request work like
    /// image uploads.
    /// </summary>
    Task<ReviewDto?> FindReviewByClientRequestIdAsync(string userId, string clientRequestId);
    Task<PaginatedResult<ReviewDto>> GetReviewsAsync(int pingActivityId, string scope, string userId, PaginationParams pagination);
    Task<PaginatedResult<ExploreReviewDto>> GetExploreReviewsAsync(ExploreReviewsFilterDto filter, string? userId, PaginationParams pagination);
    Task LikeReviewAsync(int reviewId, string userId);
    Task UnlikeReviewAsync(int reviewId, string userId);

    /// <summary>
    /// Adds one unique sticker reaction to a review (up to
    /// <see cref="ReviewService.MaxReactionsPerUserPerReview"/> per user per review). Throws
    /// <see cref="KeyNotFoundException"/> if the review or sticker is missing, and
    /// <see cref="ArgumentException"/> if the user does not own the sticker, already
    /// used it, or is at the cap. The author is notified only on the user's first reaction.
    /// </summary>
    Task AddReviewReactionAsync(int reviewId, string userId, string stickerId);

    /// <summary>
    /// Removes the user's reactions from a review — all of them, or only
    /// <paramref name="stickerId"/> when provided. Idempotent.
    /// </summary>
    Task RemoveReviewReactionsAsync(int reviewId, string userId, string? stickerId = null);

    /// <summary>Aggregated sticker reactions on a review, grouped by sticker.</summary>
    Task<List<ReviewReactionDto>> GetReviewReactionsAsync(int reviewId, string? userId);

    /// <summary>
    /// Aggregated sticker reactions for a batch of reviews, keyed by review id. Used by
    /// list endpoints (including other services' review lists) to populate
    /// <c>Reactions</c> with one query per page.
    /// </summary>
    Task<Dictionary<int, List<ReviewReactionDto>>> GetReactionsForReviewsAsync(List<int> reviewIds, string? userId);
    Task<PaginatedResult<ExploreReviewDto>> GetLikedReviewsAsync(string userId, PaginationParams pagination, string? sortBy = null, string? sortOrder = null);
    Task<PaginatedResult<ExploreReviewDto>> GetUserLikesAsync(string targetUserId, string viewerUserId, PaginationParams pagination, string? sortBy = null, string? sortOrder = null);
    Task<PaginatedResult<ExploreReviewDto>> GetUserReviewsAsync(string targetUserId, string currentUserId, PaginationParams pagination);
    Task<PaginatedResult<ExploreReviewDto>> GetMyReviewsAsync(string userId, PaginationParams pagination);
    Task<PaginatedResult<ExploreReviewDto>> GetFriendsFeedAsync(string userId, PaginationParams pagination);
    Task DeleteReviewAsAdminAsync(int id);
    Task<ReviewThumbnailBackfillResult> RegenerateReviewThumbnailsAsync(int afterId, int batchSize);
    Task DeleteReviewAsync(int reviewId, string userId);
    Task<ReviewDto> UpdateReviewAsync(int reviewId, string userId, UpdateReviewDto dto);
    Task<ExploreReviewDto> GetReviewByIdAsync(int reviewId, string? userId);
}

