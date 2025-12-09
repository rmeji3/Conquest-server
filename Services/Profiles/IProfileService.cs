using Conquest.Dtos.Profiles;
using Microsoft.AspNetCore.Http; // Add this for IFormFile
using Conquest.Dtos.Common;
using Conquest.Dtos.Places;
using Conquest.Dtos.Events;

namespace Conquest.Services.Profiles;

public interface IProfileService
{
    Task<PersonalProfileDto> GetMyProfileAsync(string userId);
    Task<List<ProfileDto>> SearchProfilesAsync(string query, string currentUsername);
    Task<string> UpdateProfileImageAsync(string userId, IFormFile file);
    Task<ProfileDto> GetProfileByIdAsync(string targetUserId, string currentUserId);
    Task<QuickProfileDto> GetQuickProfileAsync(string targetUserId, string currentUserId);
    Task<PaginatedResult<PlaceDetailsDto>> GetUserPlacesAsync(string targetUserId, string currentUserId, PaginationParams pagination);
    Task<PaginatedResult<EventDto>> GetUserEventsAsync(string targetUserId, string currentUserId, PaginationParams pagination);
}
