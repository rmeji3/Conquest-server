using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Ping.Data.Auth;
using Ping.Data.App;

namespace Ping.Tests;

public class BaseIntegrationTest : IClassFixture<IntegrationTestFactory>
{
    protected readonly IntegrationTestFactory _factory;
    protected readonly HttpClient _client;

    public IntegrationTestFactory Factory => _factory;
    public HttpClient Client => _client;

    public BaseIntegrationTest(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Initialize databases
        using var scope = factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        authDb.Database.EnsureCreated();
        appDb.Database.EnsureCreated();
    }

    protected string Authenticate(string username, string role = "User")
    {
        // For integration tests, we can simply simulate a token or login.
        // But since we mocked JWT config in Factory, we can generate a valid token manually
        // OR we can just add a header if we had a bypass.
        // Better: Generate a real JWT using the test key.
        
        // For integration tests, we can simply simulate a token or login.
        // But since we mocked JWT config in Factory, we can generate a valid token manually.
        
        // Let's just create a token manually.
        var userId = Guid.NewGuid().ToString();
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
            new(System.Security.Claims.ClaimTypes.Name, username),
            new(System.Security.Claims.ClaimTypes.Role, role)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("ThisIsATestSecretKeyForJWTMustBe32CharsLong!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);

        return userId;
    }
}

