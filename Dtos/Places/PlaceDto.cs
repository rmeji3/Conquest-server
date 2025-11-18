namespace Conquest.Dtos.Places
{
    public record UpsertPlaceDto(string Name, string Address, double Latitude, double Longitude);
    public record PlaceDetailsDto(
        int Id, 
        string Name, 
        string Address, 
        double Latitude, 
        double Longitude, 
        string[] Activities,
        string[] ActivityKinds
        );

}
