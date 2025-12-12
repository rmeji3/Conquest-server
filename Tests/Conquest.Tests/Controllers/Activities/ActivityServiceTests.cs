using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using Conquest.Dtos.Places;
using Conquest.Models.Places;
using Conquest.Dtos.Activities;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Conquest.Tests.Controllers.Activities;

public class ActivityServiceTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public ActivityServiceTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task CreateActivity_ShouldReturnMergedResults()
    {
        // Arrange
        // 1. Create a custom client with mocked AI service that reports a duplicate
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<Conquest.Services.AI.ISemanticService>();
                var mockSemantic = new Mock<Conquest.Services.AI.ISemanticService>();
                
                // When "Soccer" is added, if "Playing Soccer" exists, return "Playing Soccer" as the duplicate
                mockSemantic.Setup(x => x.FindDuplicateAsync(
                    It.Is<string>(s => s == "Soccer"), 
                    It.IsAny<IEnumerable<string>>()))
                    .ReturnsAsync("Playing Soccer");
                
                // For the first call ("Playing Soccer"), return null (no duplicate)
                mockSemantic.Setup(x => x.FindDuplicateAsync(
                    It.Is<string>(s => s == "Playing Soccer"), 
                    It.IsAny<IEnumerable<string>>()))
                    .ReturnsAsync((string?)null);

                services.AddScoped(_ => mockSemantic.Object);
            });
        }).CreateClient();

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        // 2. Authenticate
        // We call Authenticate on the base _client to generate the header, then copy it to our custom client
        Authenticate("user1");
        client.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;
    
        // 3. Create place first
        var placeRequest = new UpsertPlaceDto(
            "Test Place", 
            "123 Main St", 
            40.7128, 
            -74.0060, 
            PlaceVisibility.Public, 
            PlaceType.Verified
            );
        
        // Act
        var placeResponse = await client.PostAsJsonAsync("/api/places", placeRequest);
        var place = await placeResponse.Content.ReadFromJsonAsync<PlaceDetailsDto>(options);
    
        _output.WriteLine($"Place: {place}");
        Assert.NotNull(place);
        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);

        // 4. Create "Playing Soccer"
        var activityRequest1 = new CreateActivityDto(place.Id, "Playing Soccer", null);
        var activityResponse1 = await client.PostAsJsonAsync("/api/activities", activityRequest1);
        var activity1 = await activityResponse1.Content.ReadFromJsonAsync<ActivityDetailsDto>(options);

        // 5. Create "Soccer" -> Should be identified as duplicate of "Playing Soccer"
        var activityRequest2 = new CreateActivityDto(place.Id, "Soccer", null);
        var activityResponse2 = await client.PostAsJsonAsync("/api/activities", activityRequest2);
        var activity2 = await activityResponse2.Content.ReadFromJsonAsync<ActivityDetailsDto>(options);

        // Debug output
        _output.WriteLine($"Activity 1: {activity1}");
        _output.WriteLine($"Activity 2: (Should be same ID) {activity2}");

        // Assert
        Assert.NotNull(activity1);
        Assert.Equal(HttpStatusCode.OK, activityResponse1.StatusCode);
        Assert.NotNull(activity2);
        Assert.Equal(HttpStatusCode.OK, activityResponse2.StatusCode);

        // Verify IDs are SAME (Merged)
        Assert.Equal(activity1.Id, activity2.Id);
    }
}
