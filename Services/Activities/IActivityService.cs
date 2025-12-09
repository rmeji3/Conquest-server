using Conquest.Dtos.Activities;

namespace Conquest.Services.Activities;

public interface IActivityService
{
    Task<ActivityDetailsDto> CreateActivityAsync(CreateActivityDto dto);
    Task DeleteActivityAsAdminAsync(int id);
}
