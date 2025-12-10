using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Conquest.Data.Auth;
using Conquest.Data.App;

namespace Conquest.Tests;

public class BaseIntegrationTest : IClassFixture<IntegrationTestFactory>
{
    protected readonly IntegrationTestFactory _factory;
    protected readonly HttpClient _client;

    public BaseIntegrationTest(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Initialize databases
        using var scope = factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure fresh start if possible, or just ensure created
        // Note: InMemory DB persists for the lifetime of the process/provider. 
        // For shared factory, data persists between tests in the same class unless cleared.
        authDb.Database.EnsureCreated();
        appDb.Database.EnsureCreated();
    }
}
