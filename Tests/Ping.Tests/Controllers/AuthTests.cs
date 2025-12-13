using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using Ping.Dtos.Auth;

namespace Ping.Tests.Controllers;

public class AuthTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public AuthTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new LoginDto("invalid@example.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        // Debug output
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

