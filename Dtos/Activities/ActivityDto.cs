namespace Conquest.Dtos.Activities
{
    public class ActivityDto
    {
        public record CreateActivityDto(int PlaceId, string Type, string? Notes);
        public record ActivityDetailsDto(int Id, int PlaceId, string Type, string? Notes, DateTime CreatedUtc);
    }
}
