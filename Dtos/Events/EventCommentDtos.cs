using Ping.Dtos.Common;

namespace Ping.Dtos.Events;

public record EventCommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    string UserId,
    string UserName,
    string? UserProfileImageUrl,
    string? UserProfileThumbnailUrl
);

public record CreateEventCommentDto(
    string Content
);
