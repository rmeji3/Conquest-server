namespace Ping.Dtos.Events
{
    public record FriendInviteDto(
        string Id,
        string UserName,
        string FirstName,
        string LastName,
        string? ProfileImageUrl,
        string RequestStatus // "None", "Invited", "Attending"
    );
}
