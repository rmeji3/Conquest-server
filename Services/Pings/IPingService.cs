using Ping.Dtos.Common;
using Ping.Dtos.Pings;
using Ping.Models.Pings;

namespace Ping.Services.Pings;

public interface IPingService
{
    Task<PingDetailsDto> CreatePingAsync(UpsertPingDto dto, string userId);
    Task<PingDetailsDto?> GetPingByIdAsync(int id, string? userId);
    Task<PingDetailsDto?> GetPingByIdIncludingDeletedAsync(int id, string? userId);
    Task<PaginatedResult<PingDetailsDto>> SearchPingsAsync(PingSearchFilterDto filter, string? userId);
    Task<PaginatedResult<PingDetailsDto>> GetFavoritedPingsAsync(string userId, PaginationParams pagination);
    Task<List<PingDetailsDto>> GetPingsByOwnerAsync(string userId, bool onlyClaimed = false);
    Task<PingDetailsDto> UpdatePingAsync(int id, UpdatePingDto dto, string userId);

    Task DeletePingAsync(int id, string userId);
    Task DeletePingAsAdminAsync(int id);

    /// <summary>Admin-only: marks a ping as an officially verified place, or reverts it to custom.</summary>
    Task<PingDetailsDto> SetPingVerifiedAsync(int id, bool verified);
    Task AddFavoriteAsync(int id, string userId);
    Task UnfavoriteAsync(int id, string userId);
}

