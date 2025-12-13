using Ping.Dtos.Activities;

namespace Ping.Services.Activities;

public interface IActivityService
{
    Task<ActivityDetailsDto> CreateActivityAsync(CreateActivityDto dto);
    Task DeleteActivityAsAdminAsync(int id);
}

