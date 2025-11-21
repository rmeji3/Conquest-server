using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Mapbox;

public class MapboxService(HttpClient httpClient, IConfiguration config, ILogger<MapboxService> logger) : IMapboxService
{
    public async Task<string?> GetPlaceNameAsync(double lat, double lng)
    {
        var apiKey = config["Mapbox:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_MAPBOX_API_KEY")
        {
            logger.LogWarning("Mapbox API Key is missing or invalid.");
            return null;
        }

        try
        {
            var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{lng},{lat}.json?access_token={apiKey}&types=poi,address&limit=3";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Mapbox API error: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");

            foreach (var feature in features.EnumerateArray())
            {
                var placeType = feature.GetProperty("place_type");
                var isPoi = false;
                foreach (var type in placeType.EnumerateArray())
                {
                    if (type.GetString() == "poi")
                    {
                        isPoi = true;
                        break;
                    }
                }

                if (isPoi)
                {
                    var text = feature.GetProperty("text").GetString();
                    var placeName = feature.GetProperty("place_name").GetString();
                    var result = text ?? placeName;
                    
                    logger.LogInformation("Mapbox found POI: '{PlaceName}' for coordinates {Lat}, {Lng}", result, lat, lng);
                    return result;
                }
            }

            logger.LogInformation("Mapbox found NO POI (only addresses/other) for coordinates {Lat}, {Lng}. Falling back to user name.", lat, lng);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Mapbox API");
        }

        return null;
    }
}
