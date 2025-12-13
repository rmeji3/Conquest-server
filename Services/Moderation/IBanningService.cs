using System.Threading.Tasks;

namespace Ping.Services.Moderation
{
    public interface IBanningService
    {
        Task BanUserAsync(string userId, string reason, string? adminId = null);
        Task UnbanUserAsync(string userId, string? adminId = null);
        Task BanIpAsync(string ip, string reason, DateTime? expiresAt = null, string? adminId = null);
        Task UnbanIpAsync(string ip, string? adminId = null);
        Task<(bool IsBanned, string? Reason)> CheckBanAsync(string ip, string? userId = null);
        Task CheckReportThresholdAsync(string userId);
        Task<Ping.Dtos.Common.PagedResult<Ping.Dtos.Moderation.BannedUserDto>> GetBannedUsersAsync(int page = 1, int limit = 20);
        Task<Ping.Dtos.Moderation.BannedUserDto?> GetBannedUserAsync(string? userId = null, string? username = null, string? email = null);
    }
}

