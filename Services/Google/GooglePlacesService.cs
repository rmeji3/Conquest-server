using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Google;

public class GooglePlacesService(HttpClient httpClient, IConfiguration config, ILogger<GooglePlacesService> logger) : IPlaceNameService
{
    public async Task<string?> GetPlaceNameAsync(double lat, double lng)
    {
        var apiKey = config["Google:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Google API Key is missing.");
            return null;
        }

        try
        {
            var requestBody = new
            {
                maxResultCount = 1,
                locationRestriction = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = lat,
                            longitude = lng
                        },
                        radius = 50.0
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchNearby");
            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", "places.displayName");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Google Places API error: {StatusCode}, Body: {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("places", out var places) && places.GetArrayLength() > 0)
            {
                var firstPlace = places[0];
                if (firstPlace.TryGetProperty("displayName", out var displayNameObj))
                {
                    if (displayNameObj.TryGetProperty("text", out var textProp))
                    {
                        var placeName = textProp.GetString();
                        logger.LogInformation("Google Places found POI: '{PlaceName}' for coordinates {Lat}, {Lng}", placeName, lat, lng);
                        return placeName;
                    }
                }
            }

            logger.LogInformation("Google Places found NO POI for coordinates {Lat}, {Lng}. Falling back to user name.", lat, lng);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Google Places API");
            return null;
        }
    }
}
