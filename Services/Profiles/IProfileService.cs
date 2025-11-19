using Conquest.Dtos.Profiles;

namespace Conquest.Services.Profiles;

public interface IProfileService
{
    Task<PersonalProfileDto> GetMyProfileAsync(string userId);
    Task<List<ProfileDto>> SearchProfilesAsync(string query, string currentUsername);
}
