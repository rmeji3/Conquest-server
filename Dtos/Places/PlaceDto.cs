using Conquest.Dtos.Activities;
using Conquest.Models.Places;

namespace Conquest.Dtos.Places
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
        ActivitySummaryDto[] Activities,
        string[] ActivityKinds
        );

}
