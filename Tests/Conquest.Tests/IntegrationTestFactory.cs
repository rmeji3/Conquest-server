using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Conquest.Data.Auth;
using Conquest.Data.App;
using StackExchange.Redis;

namespace Conquest.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Add test configuration for JWT
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsATestSecretKeyForJWTMustBe32CharsLong!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenMinutes"] = "60"
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove the original DbContext registrations
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.RemoveAll<AppDbContext>();
            
            // Add InMemory DbContexts
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestAuthDb_" + Guid.NewGuid().ToString());
            });

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestAppDb_" + Guid.NewGuid().ToString());
            });

            // Replace Redis with Mock (if not already skipped by Program.cs)
            services.RemoveAll<IConnectionMultiplexer>();
            var mockMultiplexer = new Moq.Mock<IConnectionMultiplexer>();
            mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
            services.AddSingleton(mockMultiplexer.Object);

            // Replace IDistributedCache with in-memory version
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Replace IRedisService with Mock
            services.RemoveAll<Conquest.Services.Redis.IRedisService>();
            services.AddScoped(_ => new Moq.Mock<Conquest.Services.Redis.IRedisService>().Object);

            // Replace ISemanticService with Mock
            services.RemoveAll<Conquest.Services.AI.ISemanticService>();
            services.AddScoped(_ => new Moq.Mock<Conquest.Services.AI.ISemanticService>().Object);

            // Replace IModerationService with Mock
            services.RemoveAll<Conquest.Services.Moderation.IModerationService>();
            services.AddScoped(_ => new Moq.Mock<Conquest.Services.Moderation.IModerationService>().Object);

            // Add Mock IChatCompletionService
            services.AddSingleton(new Moq.Mock<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>().Object);
        });
    }
}
