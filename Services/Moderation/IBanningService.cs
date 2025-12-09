using System.Threading.Tasks;

namespace Conquest.Services.Moderation
{
    public interface IBanningService
    {
        Task BanUserAsync(string userId, string reason, string? adminId = null);
        Task UnbanUserAsync(string userId, string? adminId = null);
        Task BanIpAsync(string ip, string reason, DateTime? expiresAt = null, string? adminId = null);
        Task UnbanIpAsync(string ip, string? adminId = null);
        Task<(bool IsBanned, string? Reason)> CheckBanAsync(string ip, string? userId = null);
        Task CheckReportThresholdAsync(string userId);
    }
}
