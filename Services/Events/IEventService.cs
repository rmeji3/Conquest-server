using Conquest.Dtos.Events;
using Conquest.Models.Events;

namespace Conquest.Services.Events;

public interface IEventService
{
    Task<EventDto> CreateEventAsync(CreateEventDto dto, string userId);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<List<EventDto>> GetMyEventsAsync(string userId);
    Task<List<EventDto>> GetEventsAttendingAsync(string userId);
    Task<List<EventDto>> GetPublicEventsAsync(double minLat, double maxLat, double minLng, double maxLng);
    Task<bool> DeleteEventAsync(int id, string userId);
    Task<bool> JoinEventAsync(int id, string userId);
    Task<bool> LeaveEventAsync(int id, string userId);
}
