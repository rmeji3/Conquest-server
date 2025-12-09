using Conquest.Models.Business;

namespace Conquest.Dtos.Business
{
    public record CreateClaimDto(int PlaceId, string Proof);

    public record ClaimDto(
        int Id,
        int PlaceId,
        string PlaceName,
        string UserId,
        string UserName, // Populated if we fetch User info
        string Proof,
        ClaimStatus Status,
        DateTime CreatedUtc,
        DateTime? ReviewedUtc
    );

    // For updates if needed, or simple approve/reject actions
    public record UpdateClaimStatusDto(ClaimStatus Status);
}
