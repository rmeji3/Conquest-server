namespace Conquest.Services.Google;

public interface IPlaceNameService
{
    Task<string?> GetPlaceNameAsync(double lat, double lng);
}
