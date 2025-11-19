using Conquest.Dtos.Friends;

namespace Conquest.Services.Friends;

public interface IFriendService
{
    Task<IReadOnlyList<string>> GetFriendIdsAsync(string userId);
    Task<List<FriendSummaryDto>> GetMyFriendsAsync(string userId);
    Task<string> AddFriendAsync(string userId, string friendUsername);
    Task<string> AcceptFriendAsync(string userId, string friendUsername);
    Task<List<FriendSummaryDto>> GetIncomingRequestsAsync(string userId);
    Task<string> RemoveFriendAsync(string userId, string friendUsername);
}