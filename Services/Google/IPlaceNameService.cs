namespace Conquest.Services.Google;

public record GooglePlaceInfo(string Name, string? Address, double? Lat, double? Lng);

public interface IPlaceNameService
{
    Task<string?> GetPlaceNameAsync(double lat, double lng);
    Task<List<GooglePlaceInfo>> SearchPlacesAsync(string query, double lat, double lng, double radiusKm);
}
