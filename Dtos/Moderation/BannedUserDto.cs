namespace Conquest.Dtos.Moderation
{
    public record BannedUserDto(
        string Id,
        string Username,
        string Email,
        string? BanReason,
        int BanCount
    );
}
