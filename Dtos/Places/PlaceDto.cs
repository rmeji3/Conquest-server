using Ping.Dtos.Activities;
using Ping.Models.Places;
using Ping.Models.Business;

namespace Ping.Dtos.Places
{
    public record UpsertPlaceDto(string Name, string? Address, double Latitude, double Longitude, PlaceVisibility Visibility, PlaceType Type);
    public record PlaceDetailsDto(
        int Id, 
        string Name, 
        string Address, 
        double Latitude, 
        double Longitude, 
        PlaceVisibility Visibility,
        PlaceType Type,
        bool IsOwner,
        bool IsFavorited,
        int Favorites,
        ActivitySummaryDto[] Activities,
        string[] ActivityKinds,
        ClaimStatus? ClaimStatus = null,
        bool IsClaimed = false
        );

}

