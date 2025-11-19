using Conquest.Dtos.Profiles;
using Conquest.Models.AppUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Profiles;

public class ProfileService(UserManager<AppUser> userManager, ILogger<ProfileService> logger) : IProfileService
{
    public async Task<PersonalProfileDto> GetMyProfileAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("GetMyProfile failed: User {UserId} not found.", userId);
            throw new KeyNotFoundException("User not found.");
        }

        logger.LogDebug("Retrieved profile for {UserName}", user.UserName);

        return new PersonalProfileDto(
            user.Id,
            user.UserName!,
            user.FirstName,
            user.LastName,
            user.ProfileImageUrl,
            user.Email!
        );
    }

    public async Task<List<ProfileDto>> SearchProfilesAsync(string query, string currentUsername)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Username query parameter is required.");

        var normalized = query.ToUpper(); // match Identity normalization

        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => u.NormalizedUserName!.StartsWith(normalized)
            && u.NormalizedUserName != currentUsername.ToUpper()) // exclude yourself
            .OrderBy(u => u.UserName)
            .Take(15)
            .Select(u => new ProfileDto(
                u.Id,
                u.UserName!,
                u.FirstName,
                u.LastName,
                u.ProfileImageUrl
            ))
            .ToListAsync();

        logger.LogDebug("Profile search for '{Query}' returned {Count} results.", query, users.Count);

        return users;
    }
}
