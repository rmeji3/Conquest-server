namespace Conquest.Services.Mapbox;

public interface IMapboxService
{
    Task<string?> GetPlaceNameAsync(double lat, double lng);
}
